using SkiaSharp;
using SkiaSharp.Views.Desktop;
using SkiaSharp.Views.WPF;
using System;
using System.Windows;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;

namespace mint.paint
{
    public class DrawingManager
    {
        private SKElement drawingSurface;
        private LayerManager layerManager;
        private GridManager gridManager;
        private AdvancedColorPicker colorPicker;

        private SKPaint currentPaint;
        private UndoRedoManager undoRedoManager;
        private BrushManager brushManager;
        private DrawingToolsManager drawingTools;
        private SelectionManager selectionManager;
        
        private SKBitmap cachedComposite;
        private bool needsRedraw = true;
        private System.Windows.Threading.DispatcherTimer drawThrottle;
        private DateTime lastDrawTime = DateTime.MinValue;

        // ????????? ?????????
        private bool isDrawing = false;
        private bool isRightButtonPressed = false;
        private SKPoint lastPoint;
        private SKPoint currentPoint;
        private ToolsPanel.ToolType currentTool = ToolsPanel.ToolType.Brush;
        private SKRect textRect;
        
        private bool isTextInputActive = false;
        private SKPoint textCursorPosition;
        private bool textCursorVisible = true;
        private System.Windows.Threading.DispatcherTimer textCursorTimer;
        
        public event Action<Point, double, double> TextAreaSelected;
        public event Action<Point, string> TextApplied;

        // ?????? ?? ViewportManager ??? ???? ? ???????????????
        private ViewportManager viewportManager;

        public event Action<string> StatusChanged;
        public event Action<string> CoordinatesChanged;
        public event Action<ToolsPanel.ToolType> ToolChanged;
        public event Action<SKRect?> SelectionChanged;
        public event Action LayersChanged;

        public DrawingManager(SKElement drawingSurface, ViewportManager viewportManager)
        {
            this.drawingSurface = drawingSurface;
            this.viewportManager = viewportManager;
            this.selectionManager = new SelectionManager();
            this.colorPicker = new AdvancedColorPicker();
            Initialize();
        }

        private void Initialize()
        {
            undoRedoManager = new UndoRedoManager();
            brushManager = new BrushManager();
            drawingTools = new DrawingToolsManager();

            currentPaint = new SKPaint
            {
                Color = brushManager.PrimaryColor,
                StrokeWidth = (float)brushManager.Size,
                IsAntialias = true,
                StrokeCap = SKStrokeCap.Round,
                Style = SKPaintStyle.Stroke
            };

            // ????????????? ?? ????????? ????????
            if (viewportManager != null)
            {
                var lastViewportUpdate = DateTime.MinValue;
                viewportManager.OnViewportChanged += () => 
                {
                    var now = DateTime.Now;
                    if ((now - lastViewportUpdate).TotalMilliseconds >= 16)
                    {
                        drawingSurface.InvalidateVisual();
                        lastViewportUpdate = now;
                    }
                };
            }
            
            // ?????? ??? ??????? ???????
            textCursorTimer = new System.Windows.Threading.DispatcherTimer();
            textCursorTimer.Interval = TimeSpan.FromMilliseconds(500);
            textCursorTimer.Tick += (s, e) =>
            {
                textCursorVisible = !textCursorVisible;
                if (isTextInputActive) InvalidateCanvas();
            };
            
            // ????????????? ???????? ?????? ????????? ??? ????????????? ??????
            drawingSurface?.InvalidateVisual();
        }

        public void InitializeCanvas(int width, int height)
        {
            // ??????????? ?????? ?????? ??? ??????? ????????
            width = Math.Max(800, Math.Min(width, 1920));
            height = Math.Max(600, Math.Min(height, 1080));
            layerManager = new LayerManager(width, height);
            gridManager = new GridManager(viewportManager);

            layerManager.LayersChanged += () =>
            {
                LayersChanged?.Invoke();
                InvalidateCanvas();
            };

            layerManager.ActiveLayerChanged += (layer) =>
            {
                StatusChanged?.Invoke($"???????? ????: {layer.Name}");
            };

            // ????????? ????????? ?????????
            undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);
        }

        public void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            try
            {
                SKSurface surface = e.Surface;
                SKCanvas canvas = surface.Canvas;
                canvas.Clear(new SKColor(192, 192, 192));

                // ?????? ????? ? ?????
                if (layerManager != null)
                {
                    var canvasRect = new SKRect(0, 0, layerManager.ActiveLayer.Bitmap.Width, layerManager.ActiveLayer.Bitmap.Height);
                    
                    canvas.Save();
                    canvas.Translate((float)viewportManager.CanvasOffset.X, (float)viewportManager.CanvasOffset.Y);
                    canvas.Scale((float)viewportManager.ZoomLevel);
                    
                    // ????
                    using var shadowPaint = new SKPaint
                    {
                        Color = new SKColor(0, 0, 0, 80),
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 5)
                    };
                    canvas.DrawRect(canvasRect.Left + 5, canvasRect.Top + 5, canvasRect.Width, canvasRect.Height, shadowPaint);
                    
                    // ????? ??? ?????? ? ?????????
                    DrawCheckerboard(canvas, canvasRect);
                    
                    // ????? ?????
                    using var borderPaint = new SKPaint { Color = new SKColor(128, 128, 128), Style = SKPaintStyle.Stroke, StrokeWidth = 1 };
                    canvas.DrawRect(canvasRect, borderPaint);
                    
                    canvas.Restore();
                }

                if (layerManager == null && e.Info.Width > 0 && e.Info.Height > 0)
                {
                    InitializeCanvas(800, 600);
                    StatusChanged?.Invoke("????? ???????????????");
                }

                if (layerManager != null)
                {
                    // ????????? ????????????? ????????
                    if (viewportManager != null &&
                        (viewportManager.ZoomLevel != 1.0 || viewportManager.CanvasOffset.X != 0 || viewportManager.CanvasOffset.Y != 0))
                    {
                        canvas.Save();

                        // ???????? (???????????????)
                        canvas.Translate((float)viewportManager.CanvasOffset.X, (float)viewportManager.CanvasOffset.Y);

                        // ???????????????
                        canvas.Scale((float)viewportManager.ZoomLevel);

                        // ?????? ????
                        layerManager.DrawAllLayers(canvas);
                        // ?????? ????? ?????? ?????
                        if (gridManager.ShowGrid)
                        {
                            gridManager.DrawGrid(canvas, new SKSize(e.Info.Width, e.Info.Height), layerManager.ActiveLayer.Bitmap.Width, layerManager.ActiveLayer.Bitmap.Height);
                        }

                        // ?????? ????????? ?????? ?????
                        selectionManager.DrawSelection(canvas, (float)viewportManager.ZoomLevel, currentTool == ToolsPanel.ToolType.SelectionMove);
                        
                        // ?????? ????????? ?????? ? ?????
                        if (isTextInputActive)
                        {
                            using var textPaint = new SKPaint
                            {
                                Color = new SKColor(brushManager.PrimaryColor.Red, brushManager.PrimaryColor.Green, brushManager.PrimaryColor.Blue, currentTextOpacity),
                                TextSize = GetCurrentTextSize(),
                                IsAntialias = true,
                                Typeface = GetCurrentTextTypeface()
                            };
                            
                            if (!string.IsNullOrEmpty(currentTextInput))
                            {
                                float y = textCursorPosition.Y + GetCurrentTextSize() * 1.2f;
                                var textWidth = textPaint.MeasureText(currentTextInput);
                                float x = currentTextAlignment switch
                                {
                                    TextAlignment.Center => textCursorPosition.X - textWidth / 2,
                                    TextAlignment.Right => textCursorPosition.X - textWidth,
                                    _ => textCursorPosition.X
                                };
                                canvas.DrawText(currentTextInput, x, y, textPaint);
                                if (currentTextUnderline || currentTextStrikethrough)
                                {
                                    using var linePaint = new SKPaint { Color = new SKColor(brushManager.PrimaryColor.Red, brushManager.PrimaryColor.Green, brushManager.PrimaryColor.Blue, currentTextOpacity), StrokeWidth = Math.Max(1, GetCurrentTextSize() * 0.08f), IsAntialias = true };
                                    if (currentTextUnderline) canvas.DrawLine(x, y + GetCurrentTextSize() * 0.15f, x + textWidth, y + GetCurrentTextSize() * 0.15f, linePaint);
                                    if (currentTextStrikethrough) canvas.DrawLine(x, y - GetCurrentTextSize() * 0.35f, x + textWidth, y - GetCurrentTextSize() * 0.35f, linePaint);
                                }
                            }
                            
                            if (textCursorVisible)
                            {
                                var textWidth = textPaint.MeasureText(currentTextInput ?? "");
                                float x = currentTextAlignment switch
                                {
                                    TextAlignment.Center => textCursorPosition.X - textWidth / 2,
                                    TextAlignment.Right => textCursorPosition.X - textWidth,
                                    _ => textCursorPosition.X
                                };
                                using var cursorPaint = new SKPaint
                                {
                                    Color = SKColors.Black,
                                    StrokeWidth = 2,
                                    IsAntialias = true
                                };
                                canvas.DrawLine(x + textWidth, textCursorPosition.Y, 
                                              x + textWidth, textCursorPosition.Y + GetCurrentTextSize() * 1.2f, cursorPaint);
                            }
                        }

                        // ?????? ????????????? ??????
                        if (currentTool == ToolsPanel.ToolType.Text && isDrawing)
                        {
                            using var rectPaint = new SKPaint
                            {
                                Color = SKColors.DodgerBlue,
                                Style = SKPaintStyle.Stroke,
                                StrokeWidth = 2,
                                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
                            };
                            canvas.DrawRect(textRect, rectPaint);
                        }

                        // ?????? ?????
                        if (isDrawing && (currentTool == ToolsPanel.ToolType.Line || 
                                         currentTool == ToolsPanel.ToolType.Rectangle || 
                                         currentTool == ToolsPanel.ToolType.Circle))
                        {
                            using var previewPaint = new SKPaint
                            {
                                Color = currentPaint.Color,
                                Style = SKPaintStyle.Stroke,
                                StrokeWidth = currentPaint.StrokeWidth,
                                IsAntialias = true,
                                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
                            };

                            if (currentTool == ToolsPanel.ToolType.Line)
                            {
                                canvas.DrawLine(lastPoint, currentPoint, previewPaint);
                            }
                            else if (currentTool == ToolsPanel.ToolType.Rectangle)
                            {
                                var rect = new SKRect(lastPoint.X, lastPoint.Y, currentPoint.X, currentPoint.Y);
                                canvas.DrawRect(rect, previewPaint);
                            }
                            else if (currentTool == ToolsPanel.ToolType.Circle)
                            {
                                var rect = new SKRect(lastPoint.X, lastPoint.Y, currentPoint.X, currentPoint.Y);
                                canvas.DrawOval(rect, previewPaint);
                            }
                        }

                        if (isDrawing && currentTool == ToolsPanel.ToolType.Gradient)
                        {
                            using var previewPaint = new SKPaint
                            {
                                Style = SKPaintStyle.Stroke,
                                StrokeWidth = 2,
                                IsAntialias = true,
                                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
                            };
                            previewPaint.Shader = SKShader.CreateLinearGradient(
                                lastPoint,
                                currentPoint,
                                new[] { brushManager.PrimaryColor, brushManager.SecondaryColor },
                                null,
                                SKShaderTileMode.Clamp
                            );
                            canvas.DrawLine(lastPoint, currentPoint, previewPaint);
                        }

                        canvas.Restore();
                    }
                    else
                    {
                        // ?????? ????
                        layerManager.DrawAllLayers(canvas);

                        // ?????? ?????
                        if (gridManager.ShowGrid)
                        {
                            gridManager.DrawGrid(canvas, new SKSize(e.Info.Width, e.Info.Height), layerManager.ActiveLayer.Bitmap.Width, layerManager.ActiveLayer.Bitmap.Height);
                        }

                        // ?????? ?????????
                        selectionManager.DrawSelection(canvas, 1f, currentTool == ToolsPanel.ToolType.SelectionMove);
                        
                        // ?????? ????????? ?????? ? ?????
                        if (isTextInputActive)
                        {
                            using var textPaint = new SKPaint
                            {
                                Color = new SKColor(brushManager.PrimaryColor.Red, brushManager.PrimaryColor.Green, brushManager.PrimaryColor.Blue, currentTextOpacity),
                                TextSize = GetCurrentTextSize(),
                                IsAntialias = true,
                                Typeface = GetCurrentTextTypeface()
                            };
                            
                            if (!string.IsNullOrEmpty(currentTextInput))
                            {
                                float y = textCursorPosition.Y + GetCurrentTextSize() * 1.2f;
                                var textWidth = textPaint.MeasureText(currentTextInput);
                                float x = currentTextAlignment switch
                                {
                                    TextAlignment.Center => textCursorPosition.X - textWidth / 2,
                                    TextAlignment.Right => textCursorPosition.X - textWidth,
                                    _ => textCursorPosition.X
                                };
                                canvas.DrawText(currentTextInput, x, y, textPaint);
                                if (currentTextUnderline || currentTextStrikethrough)
                                {
                                    using var linePaint = new SKPaint { Color = new SKColor(brushManager.PrimaryColor.Red, brushManager.PrimaryColor.Green, brushManager.PrimaryColor.Blue, currentTextOpacity), StrokeWidth = Math.Max(1, GetCurrentTextSize() * 0.08f), IsAntialias = true };
                                    if (currentTextUnderline) canvas.DrawLine(x, y + GetCurrentTextSize() * 0.15f, x + textWidth, y + GetCurrentTextSize() * 0.15f, linePaint);
                                    if (currentTextStrikethrough) canvas.DrawLine(x, y - GetCurrentTextSize() * 0.35f, x + textWidth, y - GetCurrentTextSize() * 0.35f, linePaint);
                                }
                            }
                            
                            if (textCursorVisible)
                            {
                                var textWidth = textPaint.MeasureText(currentTextInput ?? "");
                                float x = currentTextAlignment switch
                                {
                                    TextAlignment.Center => textCursorPosition.X - textWidth / 2,
                                    TextAlignment.Right => textCursorPosition.X - textWidth,
                                    _ => textCursorPosition.X
                                };
                                using var cursorPaint = new SKPaint
                                {
                                    Color = SKColors.Black,
                                    StrokeWidth = 2,
                                    IsAntialias = true
                                };
                                canvas.DrawLine(x + textWidth, textCursorPosition.Y, 
                                              x + textWidth, textCursorPosition.Y + GetCurrentTextSize() * 1.2f, cursorPaint);
                            }
                        }

                        // ?????? ????????????? ??????
                        if (currentTool == ToolsPanel.ToolType.Text && isDrawing)
                        {
                            using var rectPaint = new SKPaint
                            {
                                Color = SKColors.DodgerBlue,
                                Style = SKPaintStyle.Stroke,
                                StrokeWidth = 2,
                                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
                            };
                            canvas.DrawRect(textRect, rectPaint);
                        }

                        // ?????? ?????
                        if (isDrawing && (currentTool == ToolsPanel.ToolType.Line || 
                                         currentTool == ToolsPanel.ToolType.Rectangle || 
                                         currentTool == ToolsPanel.ToolType.Circle))
                        {
                            using var previewPaint = new SKPaint
                            {
                                Color = currentPaint.Color,
                                Style = SKPaintStyle.Stroke,
                                StrokeWidth = currentPaint.StrokeWidth,
                                IsAntialias = true,
                                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
                            };

                            if (currentTool == ToolsPanel.ToolType.Line)
                            {
                                canvas.DrawLine(lastPoint, currentPoint, previewPaint);
                            }
                            else if (currentTool == ToolsPanel.ToolType.Rectangle)
                            {
                                var rect = new SKRect(lastPoint.X, lastPoint.Y, currentPoint.X, currentPoint.Y);
                                canvas.DrawRect(rect, previewPaint);
                            }
                            else if (currentTool == ToolsPanel.ToolType.Circle)
                            {
                                var rect = new SKRect(lastPoint.X, lastPoint.Y, currentPoint.X, currentPoint.Y);
                                canvas.DrawOval(rect, previewPaint);
                            }
                        }

                        if (isDrawing && currentTool == ToolsPanel.ToolType.Gradient)
                        {
                            using var previewPaint = new SKPaint
                            {
                                Style = SKPaintStyle.Stroke,
                                StrokeWidth = 2,
                                IsAntialias = true,
                                PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
                            };
                            previewPaint.Shader = SKShader.CreateLinearGradient(
                                lastPoint,
                                currentPoint,
                                new[] { brushManager.PrimaryColor, brushManager.SecondaryColor },
                                null,
                                SKShaderTileMode.Clamp
                            );
                            canvas.DrawLine(lastPoint, currentPoint, previewPaint);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                e.Surface.Canvas.Clear(SKColors.White);
                System.Diagnostics.Debug.WriteLine($"Paint error: {ex.Message}");
            }
        }

        public void HandleMouseDown(Point screenPosition, bool isSpacePressed, bool isRightButton = false)
        {
            if (layerManager == null)
            {
                StatusChanged?.Invoke("??????: ????? ?? ???????????????. ?????????? ???????? ?????? ????.");
                return;
            }
            if (layerManager.ActiveLayer == null || currentPaint == null)
            {
                StatusChanged?.Invoke("??????: ??? ????????? ????");
                return;
            }

            // ???? ???????? ??????????????? - ?? ???????????? ?????????
            if (isSpacePressed) return;

            // ??????????? ???????? ?????????? ? ?????????? ??????
            Point canvasPosition = screenPosition;
            if (viewportManager != null)
            {
                canvasPosition = viewportManager.ScreenToCanvas(screenPosition);
            }

            // ????????? ???????? ? ?????
            if (gridManager.SnapToGrid)
            {
                canvasPosition = gridManager.SnapToGridPoint(canvasPosition);
            }

            // ????????? ??????????? ?????????
            if (currentTool == ToolsPanel.ToolType.Selection || currentTool == ToolsPanel.ToolType.EllipseSelection || currentTool == ToolsPanel.ToolType.Lasso || currentTool == ToolsPanel.ToolType.SelectionMove)
            {
                selectionManager.SetSourceBitmap(layerManager.ActiveLayer.IsVisible ? layerManager.ActiveLayer.Bitmap : null);
                selectionManager.SetZoom((float)viewportManager.ZoomLevel);
                if (currentTool == ToolsPanel.ToolType.EllipseSelection)
                    selectionManager.SetSelectionType(SelectionType.Ellipse);
                else if (currentTool == ToolsPanel.ToolType.Lasso)
                    selectionManager.SetSelectionType(SelectionType.Lasso);
                else
                    selectionManager.SetSelectionType(SelectionType.Rectangle);
                selectionManager.HandleMouseDown(canvasPosition, isRightButton);
                InvalidateCanvas();
                return;
            }

            // ????????? ???????
            if (currentTool == ToolsPanel.ToolType.MagicWand)
            {
                var rect = drawingTools.MagicWandSelect(layerManager.ActiveLayer.Bitmap, new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y));
                if (rect.HasValue)
                {
                    bool isShiftPressed = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
                    if (!isShiftPressed)
                    {
                        selectionManager.SetSelection(rect.Value);
                        SelectionChanged?.Invoke(rect);
                        StatusChanged?.Invoke("??????? ????????");
                    }
                    else if (selectionManager.HasSelection)
                    {
                        var currentBounds = selectionManager.SelectionBounds.Value;
                        var newBounds = SKRect.Union(currentBounds, rect.Value);
                        selectionManager.SetSelection(newBounds);
                        SelectionChanged?.Invoke(newBounds);
                        StatusChanged?.Invoke("??????? ?????????");
                    }
                    else
                    {
                        selectionManager.SetSelection(rect.Value);
                        SelectionChanged?.Invoke(rect);
                        StatusChanged?.Invoke("??????? ????????");
                    }
                    InvalidateCanvas();
                }
                return;
            }

            // ?????????? ?????? - ???????? ????????? ???????
            if (currentTool == ToolsPanel.ToolType.Text)
            {
                isDrawing = true;
                lastPoint = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);
                textRect = new SKRect(lastPoint.X, lastPoint.Y, lastPoint.X, lastPoint.Y);
                return;
            }

            // ???????
            if (currentTool == ToolsPanel.ToolType.Eyedropper)
            {
                PickColorFromCanvas(canvasPosition);
                return;
            }

            // ????????? ??????? (Alt + ????? ??????)
            if ((Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
            {
                PickColorFromCanvas(canvasPosition);
                return;
            }

            // ??????? ?????????
            if (!isDrawing)
            {
                undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);
            }

            isDrawing = true;
            isRightButtonPressed = isRightButton;
            lastPoint = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);

            UpdateStatusText();

            if (currentTool == ToolsPanel.ToolType.Fill)
            {
                drawingTools.ApplyFill(layerManager.ActiveLayer.Bitmap, lastPoint, brushManager.PrimaryColor);
                InvalidateCanvas();
                StatusChanged?.Invoke("??????? ??????");
            }
        }

        public void HandleMouseMove(Point screenPosition)
        {
            // ??????????? ??????????
            Point canvasPosition = screenPosition;
            if (viewportManager != null)
            {
                canvasPosition = viewportManager.ScreenToCanvas(screenPosition);
            }

            // ????????? ???????? ? ?????
            if (gridManager != null && gridManager.SnapToGrid)
            {
                canvasPosition = gridManager.SnapToGridPoint(canvasPosition);
            }

            // ????????? ?????????? ? ??????????
            string coordinates = $"X: {(int)canvasPosition.X}, Y: {(int)canvasPosition.Y}";
            if (viewportManager != null && viewportManager.ZoomLevel != 1.0)
            {
                coordinates += $" (Zoom: {viewportManager.ZoomLevel:0%})";
            }
            if (gridManager.SnapToGrid)
            {
                coordinates += " [Snap]";
            }
            CoordinatesChanged?.Invoke(coordinates);

            // ????????? ??????????? ?????????
            if (currentTool == ToolsPanel.ToolType.Selection || currentTool == ToolsPanel.ToolType.EllipseSelection || currentTool == ToolsPanel.ToolType.Lasso || currentTool == ToolsPanel.ToolType.SelectionMove)
            {
                selectionManager.SetZoom((float)viewportManager.ZoomLevel);
                selectionManager.HandleMouseMove(canvasPosition);
                InvalidateCanvas();
                return;
            }

            // ?????????? ?????? - ?????? ?????????????
            if (currentTool == ToolsPanel.ToolType.Text && isDrawing)
            {
                var textPoint = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);
                textRect = new SKRect(
                    Math.Min(lastPoint.X, textPoint.X),
                    Math.Min(lastPoint.Y, textPoint.Y),
                    Math.Max(lastPoint.X, textPoint.X),
                    Math.Max(lastPoint.Y, textPoint.Y)
                );
                InvalidateCanvas();
                return;
            }

            if (!isDrawing) return;
            if (layerManager?.ActiveLayer == null) return;

            currentPoint = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);

            if (currentTool == ToolsPanel.ToolType.Brush || currentTool == ToolsPanel.ToolType.Eraser)
            {
                using var canvas = new SKCanvas(layerManager.ActiveLayer.Bitmap);
                var drawColor = isRightButtonPressed ? brushManager.SecondaryColor : brushManager.PrimaryColor;
                if (currentTool == ToolsPanel.ToolType.Brush && (brushManager.CurrentBrushType == BrushType.Watercolor || brushManager.CurrentBrushType == BrushType.Oil || brushManager.CurrentBrushType == BrushType.Textured || brushManager.CurrentBrushType == BrushType.Pixel || brushManager.CurrentBrushType == BrushType.Drops))
                {
                    var distance = System.Math.Sqrt(System.Math.Pow(currentPoint.X - lastPoint.X, 2) + System.Math.Pow(currentPoint.Y - lastPoint.Y, 2));
                    var steps = (int)System.Math.Max(1, distance / (brushManager.Size * 0.3));
                    for (int i = 0; i <= steps; i++)
                    {
                        var t = i / (float)steps;
                        var interpPoint = new SKPoint(
                            lastPoint.X + (currentPoint.X - lastPoint.X) * t,
                            lastPoint.Y + (currentPoint.Y - lastPoint.Y) * t
                        );
                        if (selectionManager.HasSelection && !IsPointInSelection(interpPoint)) continue;
                        brushManager.DrawBrushStroke(canvas, interpPoint, lastPoint);
                    }
                }
                else
                {
                    if (selectionManager.HasSelection)
                    {
                        canvas.Save();
                        using var clipPath = new SKPath();
                        var bounds = selectionManager.SelectionBounds.Value;
                        if (selectionManager.CurrentSelectionType == SelectionType.Ellipse)
                        {
                            clipPath.AddOval(bounds);
                        }
                        else if (selectionManager.CurrentSelectionType == SelectionType.Lasso && selectionManager.LassoPoints.Count > 2)
                        {
                            clipPath.MoveTo(selectionManager.LassoPoints[0]);
                            for (int i = 1; i < selectionManager.LassoPoints.Count; i++)
                            {
                                clipPath.LineTo(selectionManager.LassoPoints[i]);
                            }
                            clipPath.Close();
                        }
                        else
                        {
                            clipPath.AddRect(bounds);
                        }
                        canvas.ClipPath(clipPath);
                    }
                    
                    var linePaint = new SKPaint
                    {
                        Color = currentTool == ToolsPanel.ToolType.Eraser ? SKColors.Transparent : drawColor,
                        StrokeWidth = (float)brushManager.Size,
                        IsAntialias = true,
                        StrokeCap = SKStrokeCap.Round,
                        Style = SKPaintStyle.Stroke,
                        BlendMode = currentTool == ToolsPanel.ToolType.Eraser ? SKBlendMode.Clear : SKBlendMode.SrcOver
                    };
                    if (brushManager.Hardness < 100)
                    {
                        linePaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, (float)((100 - brushManager.Hardness) / 100.0 * brushManager.Size / 4));
                    }
                    var distance = System.Math.Sqrt(System.Math.Pow(currentPoint.X - lastPoint.X, 2) + System.Math.Pow(currentPoint.Y - lastPoint.Y, 2));
                    if (distance > brushManager.Size / 4)
                    {
                        var steps = (int)System.Math.Max(1, distance / (brushManager.Size / 4));
                        for (int i = 0; i <= steps; i++)
                        {
                            var t = i / (float)steps;
                            var interpPoint = new SKPoint(
                                lastPoint.X + (currentPoint.X - lastPoint.X) * t,
                                lastPoint.Y + (currentPoint.Y - lastPoint.Y) * t
                            );
                            if (i > 0)
                            {
                                var prevT = (i - 1) / (float)steps;
                                var prevPoint = new SKPoint(
                                    lastPoint.X + (currentPoint.X - lastPoint.X) * prevT,
                                    lastPoint.Y + (currentPoint.Y - lastPoint.Y) * prevT
                                );
                                canvas.DrawLine(prevPoint, interpPoint, linePaint);
                            }
                        }
                    }
                    else
                    {
                        canvas.DrawLine(lastPoint, currentPoint, linePaint);
                    }
                    linePaint.Dispose();
                    
                    if (selectionManager.HasSelection)
                    {
                        canvas.Restore();
                    }
                }
                InvalidateCanvas();
                lastPoint = currentPoint;
            }
            else if (currentTool == ToolsPanel.ToolType.Line || 
                     currentTool == ToolsPanel.ToolType.Rectangle || 
                     currentTool == ToolsPanel.ToolType.Circle ||
                     currentTool == ToolsPanel.ToolType.Gradient)
            {
                InvalidateCanvas();
            }
        }

        public void HandleMouseUp(Point screenPosition)
        {
            // ??????????? ??????????
            Point canvasPosition = screenPosition;
            if (viewportManager != null)
            {
                canvasPosition = viewportManager.ScreenToCanvas(screenPosition);
            }

            // ????????? ???????? ? ?????
            if (gridManager.SnapToGrid)
            {
                canvasPosition = gridManager.SnapToGridPoint(canvasPosition);
            }

            // ????????? ??????????? ?????????
            if (currentTool == ToolsPanel.ToolType.Selection || currentTool == ToolsPanel.ToolType.EllipseSelection || currentTool == ToolsPanel.ToolType.Lasso || currentTool == ToolsPanel.ToolType.SelectionMove)
            {
                selectionManager.HandleMouseUp(canvasPosition);
                SelectionChanged?.Invoke(selectionManager.SelectionBounds);
                InvalidateCanvas();
            }

            // ?????????? ?????? - ?????????? TextOverlay
            if (currentTool == ToolsPanel.ToolType.Text && isDrawing)
            {
                isDrawing = false;
                var width = Math.Abs(textRect.Width);
                var height = Math.Abs(textRect.Height);
                if (width < 50 || height < 30)
                {
                    width = 300;
                    height = 150;
                }
                TextAreaSelected?.Invoke(new Point(textRect.Left, textRect.Top), width, height);
                InvalidateCanvas();
                return;
            }

            if (isDrawing && currentTool == ToolsPanel.ToolType.Brush)
            {
                var currentPoint = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);
                if (currentPoint.X == lastPoint.X && currentPoint.Y == lastPoint.Y)
                {
                    if (selectionManager.HasSelection && !IsPointInSelection(currentPoint))
                    {
                        isDrawing = false;
                        return;
                    }
                    using var canvas = new SKCanvas(layerManager.ActiveLayer.Bitmap);
                    brushManager.DrawBrushStroke(canvas, lastPoint, lastPoint);
                    InvalidateCanvas();
                }
            }
            else if (isDrawing && currentTool != ToolsPanel.ToolType.Brush &&
                currentTool != ToolsPanel.ToolType.Eraser && currentTool != ToolsPanel.ToolType.Fill)
            {
                var currentPoint = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);

                using var canvas = new SKCanvas(layerManager.ActiveLayer.Bitmap);
                
                if (selectionManager.HasSelection)
                {
                    canvas.Save();
                    using var clipPath = new SKPath();
                    var bounds = selectionManager.SelectionBounds.Value;
                    if (selectionManager.CurrentSelectionType == SelectionType.Ellipse)
                    {
                        clipPath.AddOval(bounds);
                    }
                    else if (selectionManager.CurrentSelectionType == SelectionType.Lasso && selectionManager.LassoPoints.Count > 2)
                    {
                        clipPath.MoveTo(selectionManager.LassoPoints[0]);
                        for (int i = 1; i < selectionManager.LassoPoints.Count; i++)
                        {
                            clipPath.LineTo(selectionManager.LassoPoints[i]);
                        }
                        clipPath.Close();
                    }
                    else
                    {
                        clipPath.AddRect(bounds);
                    }
                    canvas.ClipPath(clipPath);
                }
                
                switch (currentTool)
                {
                    case ToolsPanel.ToolType.Line:
                        drawingTools.DrawLine(canvas, lastPoint, currentPoint, currentPaint);
                        break;
                    case ToolsPanel.ToolType.Rectangle:
                        drawingTools.DrawRectangle(canvas, lastPoint, currentPoint, currentPaint);
                        break;
                    case ToolsPanel.ToolType.Circle:
                        var ovalRect = new SKRect(lastPoint.X, lastPoint.Y, currentPoint.X, currentPoint.Y);
                        canvas.DrawOval(ovalRect, currentPaint);
                        break;
                    case ToolsPanel.ToolType.Gradient:
                        using (var gradientPaint = new SKPaint())
                        {
                            SKColor[] colors;
                            float[] positions = null;
                            
                            if (gradientStops.Count > 0)
                            {
                                colors = gradientStops.Select(s => s.Color).ToArray();
                                positions = gradientStops.Select(s => s.Position).ToArray();
                            }
                            else
                            {
                                colors = new[] { brushManager.PrimaryColor, brushManager.SecondaryColor };
                            }
                            
                            switch (currentGradientStyle)
                            {
                                case "Linear":
                                    gradientPaint.Shader = SKShader.CreateLinearGradient(lastPoint, currentPoint, colors, positions, SKShaderTileMode.Clamp);
                                    break;
                                case "Radial":
                                    var radius = (float)Math.Sqrt(Math.Pow(currentPoint.X - lastPoint.X, 2) + Math.Pow(currentPoint.Y - lastPoint.Y, 2));
                                    gradientPaint.Shader = SKShader.CreateRadialGradient(lastPoint, radius, colors, positions, SKShaderTileMode.Clamp);
                                    break;
                                case "Reflected":
                                    gradientPaint.Shader = SKShader.CreateLinearGradient(lastPoint, currentPoint, colors, positions, SKShaderTileMode.Mirror);
                                    break;
                            }
                            
                            var rect = selectionManager.HasSelection ? selectionManager.SelectionBounds.Value : new SKRect(0, 0, layerManager.ActiveLayer.Bitmap.Width, layerManager.ActiveLayer.Bitmap.Height);
                            if (selectionManager.HasSelection && selectionManager.CurrentSelectionType == SelectionType.Lasso && selectionManager.LassoPoints.Count > 2)
                            {
                                using var lassoPath = new SKPath();
                                lassoPath.MoveTo(selectionManager.LassoPoints[0]);
                                for (int i = 1; i < selectionManager.LassoPoints.Count; i++)
                                {
                                    lassoPath.LineTo(selectionManager.LassoPoints[i]);
                                }
                                lassoPath.Close();
                                canvas.DrawPath(lassoPath, gradientPaint);
                            }
                            else
                            {
                                canvas.DrawRect(rect, gradientPaint);
                            }
                        }
                        break;
                }
                
                if (selectionManager.HasSelection)
                {
                    canvas.Restore();
                }

                InvalidateCanvas();
            }

            isDrawing = false;
            needsRedraw = true;
            InvalidateCanvas();
            StatusChanged?.Invoke("????? ? ?????????...");
        }

        private void PickColorFromCanvas(Point canvasPosition)
        {
            if (layerManager?.ActiveLayer?.Bitmap == null) return;

            try
            {
                var bitmap = layerManager.ActiveLayer.Bitmap;

                // ????????? ???????
                if (canvasPosition.X < 0 || canvasPosition.Y < 0 ||
                    canvasPosition.X >= bitmap.Width || canvasPosition.Y >= bitmap.Height)
                {
                    StatusChanged?.Invoke("???? ?? ??????: ????? ??? ??????");
                    return;
                }

                var pixelColor = bitmap.GetPixel((int)canvasPosition.X, (int)canvasPosition.Y);
                brushManager.Color = pixelColor;
                currentPaint.Color = pixelColor;

                // ????????? ? ??????? ??????
                colorPicker.AddRecentColor(pixelColor);

                StatusChanged?.Invoke($"???? ??????: RGB({pixelColor.Red}, {pixelColor.Green}, {pixelColor.Blue})");
                SetTool(ToolsPanel.ToolType.Brush);
            }
            catch (Exception ex)
            {
                StatusChanged?.Invoke("?????? ?????? ?????");
                System.Diagnostics.Debug.WriteLine($"Eyedropper error: {ex.Message}");
            }
        }

        public void SetTool(ToolsPanel.ToolType tool)
        {
            currentTool = tool;
            brushManager.UpdatePaint(currentPaint, tool);
            ToolChanged?.Invoke(tool);
            UpdateStatusText();
        }

        private void UpdateStatusText()
        {
            string status = currentTool switch
            {
                ToolsPanel.ToolType.Brush => "????????? ??????...",
                ToolsPanel.ToolType.Eraser => "????????...",
                ToolsPanel.ToolType.Fill => "???????...",
                ToolsPanel.ToolType.Eyedropper => "????? ?????...",
                ToolsPanel.ToolType.Line => "????????? ?????...",
                ToolsPanel.ToolType.Rectangle => "????????? ??????????????...",
                ToolsPanel.ToolType.Circle => "????????? ?????...",
                ToolsPanel.ToolType.Selection => "????????? ???????...",
                ToolsPanel.ToolType.SelectionMove => "??????????? ???????...",
                ToolsPanel.ToolType.Text => "????????, ????? ???????? ?????...",
                ToolsPanel.ToolType.Gradient => "????????? ????????...",
                ToolsPanel.ToolType.MagicWand => "???????? ??????? ?????...",
                _ => "?????????..."
            };

            StatusChanged?.Invoke(status);
        }

        public void DrawText(Point position, string text, string fontFamily, float fontSize, 
                             SKColor color, bool bold, bool italic, bool underline)
        {
            DrawTextWithFormatting(position, text, fontFamily, fontSize, color, bold, italic, underline, false);
        }
        
        public void DrawTextWithFormatting(Point position, string text, string fontFamily, float fontSize, 
                             SKColor color, bool bold, bool italic, bool underline, bool strikethrough, TextAlignment alignment = TextAlignment.Left)
        {
            if (layerManager?.ActiveLayer?.Bitmap == null) return;

            undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);

            using var canvas = new SKCanvas(layerManager.ActiveLayer.Bitmap);
            
            var fontStyle = SKFontStyle.Normal;
            if (bold && italic)
                fontStyle = SKFontStyle.BoldItalic;
            else if (bold)
                fontStyle = SKFontStyle.Bold;
            else if (italic)
                fontStyle = SKFontStyle.Italic;

            SKTypeface typeface;
            if (PresetsWindow.CustomFontFamilies.ContainsKey(fontFamily))
            {
                typeface = SKTypeface.FromFamilyName(fontFamily, fontStyle);
            }
            else
            {
                typeface = SKTypeface.FromFamilyName(fontFamily, fontStyle);
            }
            
            using var textPaint = new SKPaint
            {
                Color = color,
                TextSize = fontSize,
                IsAntialias = true,
                Typeface = typeface
            };

            var lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            float y = (float)position.Y + fontSize * 1.2f;

            using var linePaint = new SKPaint
            {
                Color = color,
                StrokeWidth = Math.Max(1, fontSize * 0.08f),
                IsAntialias = true
            };

            foreach (var line in lines)
            {
                var width = textPaint.MeasureText(line);
                float x = alignment switch
                {
                    TextAlignment.Center => (float)position.X - width / 2,
                    TextAlignment.Right => (float)position.X - width,
                    _ => (float)position.X
                };
                canvas.DrawText(line, x, y, textPaint);
                
                if (underline)
                {
                    canvas.DrawLine(x, y + fontSize * 0.15f, x + width, y + fontSize * 0.15f, linePaint);
                }
                
                if (strikethrough)
                {
                    canvas.DrawLine(x, y - fontSize * 0.35f, x + width, y - fontSize * 0.35f, linePaint);
                }
                
                y += fontSize * 1.3f;
            }

            InvalidateCanvas();
            StatusChanged?.Invoke("????? ????????");
        }

        // ????? ?????? ??? ?????????? ??????
        public double GetBrushSize() => brushManager.Size;
        public double GetBrushHardness() => brushManager.Hardness;

        public void SetBrushSize(double size)
        {
            brushManager.Size = size;
            if (currentPaint != null)
            {
                if (currentTool == ToolsPanel.ToolType.Brush || currentTool == ToolsPanel.ToolType.Eraser)
                {
                    currentPaint.StrokeWidth = (float)brushManager.Size;
                }
            }
        }

        public void ChangeBrushSize(int delta)
        {
            var newSize = brushManager.Size + delta;
            newSize = Math.Max(1, Math.Min(100, newSize));
            SetBrushSize(newSize);
        }

        public void SetBrushType(BrushType type)
        {
            brushManager.CurrentBrushType = type;
        }

        public void SetDropSize(double size)
        {
            brushManager.DropSize = size;
        }

        public void SetDropCount(int count)
        {
            brushManager.DropCount = count;
        }

        public BrushType GetCurrentBrushType()
        {
            return brushManager.CurrentBrushType;
        }

        public void SetBrushHardness(double hardness)
        {
            brushManager.Hardness = hardness;
        }

        public void SwapColors()
        {
            brushManager.SwapColors();
            if (currentPaint != null && currentTool != ToolsPanel.ToolType.Eraser)
            {
                currentPaint.Color = brushManager.PrimaryColor;
            }
            StatusChanged?.Invoke("????? ???????? ???????");
        }

        // ?????? ??? ?????? ? ??????????
        public void ApplySelection()
        {
            selectionManager.ApplyAndClearSelection();
            SelectionChanged?.Invoke(null);
            InvalidateCanvas();
        }
        
        public void ClearSelection()
        {
            selectionManager.ClearSelection();
            SelectionChanged?.Invoke(null);
            InvalidateCanvas();
        }
        
        public bool IsPointInSelection(SKPoint point)
        {
            return selectionManager.IsPointInSelection(point);
        }
        
        public void SetSelectionType(SelectionType type)
        {
            selectionManager.SetSelectionType(type);
        }
        
        private bool magicWandAddMode = false;
        
        public void SetMagicWandAddMode(bool addMode)
        {
            magicWandAddMode = addMode;
        }

        public void CutSelection()
        {
            if (selectionManager.HasSelection && layerManager?.ActiveLayer?.Bitmap != null)
            {
                undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);
                selectionManager.CutSelection(layerManager.ActiveLayer.Bitmap);
                InvalidateCanvas();
                StatusChanged?.Invoke("????????? ????????");
            }
        }

        public void CopySelection()
        {
            if (selectionManager.HasSelection && layerManager?.ActiveLayer?.Bitmap != null)
            {
                selectionManager.CopySelection(layerManager.ActiveLayer.Bitmap);
                StatusChanged?.Invoke("????????? ???????????");
            }
        }

        public void PasteSelection()
        {
            if (selectionManager.HasClipboardImage && layerManager?.ActiveLayer?.Bitmap != null)
            {
                undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);
                selectionManager.PasteSelection(layerManager.ActiveLayer.Bitmap);
                InvalidateCanvas();
                StatusChanged?.Invoke("????????? ?????????");
            }
        }

        public void DeleteSelection()
        {
            if (selectionManager.HasSelection && layerManager?.ActiveLayer?.Bitmap != null)
            {
                undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);
                selectionManager.DeleteSelection(layerManager.ActiveLayer.Bitmap);
                InvalidateCanvas();
                StatusChanged?.Invoke("????????? ???????");
            }
        }

        // ???????????? ??????
        public void Undo()
        {
            if (!undoRedoManager.CanUndo || layerManager?.ActiveLayer?.Bitmap == null) return;

            var previousState = undoRedoManager.Undo(layerManager.ActiveLayer.Bitmap);
            RestoreCanvasState(previousState);
            StatusChanged?.Invoke("????????");
        }

        public void Redo()
        {
            if (!undoRedoManager.CanRedo || layerManager?.ActiveLayer?.Bitmap == null) return;

            var nextState = undoRedoManager.Redo(layerManager.ActiveLayer.Bitmap);
            RestoreCanvasState(nextState);
            StatusChanged?.Invoke("?????????");
        }

        private void RestoreCanvasState(SKBitmap state)
        {
            using (var canvas = new SKCanvas(layerManager.ActiveLayer.Bitmap))
            {
                canvas.Clear(SKColors.Transparent);
                canvas.DrawBitmap(state, 0, 0);
            }
            InvalidateCanvas();
        }

        public void NewCanvas()
        {
            if (layerManager?.ActiveLayer?.Bitmap == null) return;

            undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);

            // ??????? ??? ????
            foreach (var layer in layerManager.Layers)
            {
                layer.Clear();
            }

            // ??????? ?????????
            ClearSelection();

            InvalidateCanvas();
            StatusChanged?.Invoke("?????? ????? ?????");
        }

        public void ClearCanvas()
        {
            if (layerManager?.ActiveLayer?.Bitmap == null) return;

            undoRedoManager.SaveState(layerManager.ActiveLayer.Bitmap);

            // ??????? ???????? ????
            layerManager.ActiveLayer.Clear();

            // ??????? ?????????
            ClearSelection();

            InvalidateCanvas();
            StatusChanged?.Invoke("????? ??????");
        }

        public void ApplyBrushPreset(string preset)
        {
            switch (preset)
            {
                case "soft_round":
                    brushManager.Size = 10;
                    brushManager.Hardness = 30;
                    if (currentPaint != null)
                    {
                        currentPaint.IsAntialias = true;
                        currentPaint.StrokeCap = SKStrokeCap.Round;
                        currentPaint.StrokeWidth = (float)brushManager.Size;
                    }
                    StatusChanged?.Invoke("????????? ?????? ??????? ?????");
                    break;

                case "hard_round":
                    brushManager.Size = 8;
                    brushManager.Hardness = 100;
                    if (currentPaint != null)
                    {
                        currentPaint.IsAntialias = false;
                        currentPaint.StrokeCap = SKStrokeCap.Butt;
                        currentPaint.StrokeWidth = (float)brushManager.Size;
                    }
                    StatusChanged?.Invoke("????????? ??????? ??????? ?????");
                    break;

                default:
                    brushManager.Size = 5;
                    brushManager.Hardness = 100;
                    if (currentPaint != null)
                    {
                        currentPaint.IsAntialias = true;
                        currentPaint.StrokeCap = SKStrokeCap.Round;
                        currentPaint.StrokeWidth = (float)brushManager.Size;
                    }
                    StatusChanged?.Invoke("????????? ????? ?? ?????????");
                    break;
            }
        }

        public void ResetBrushSettings()
        {
            brushManager.Size = 5;
            brushManager.Hardness = 100;
            brushManager.PrimaryColor = SKColors.Blue;
            brushManager.SecondaryColor = SKColors.White;

            if (currentPaint != null)
            {
                currentPaint.Color = brushManager.PrimaryColor;
                currentPaint.StrokeWidth = (float)brushManager.Size;
                currentPaint.IsAntialias = true;
                currentPaint.StrokeCap = SKStrokeCap.Round;
            }

            StatusChanged?.Invoke("????????? ????? ????????");
        }

        public void SetBrushColor(SKColor color)
        {
            brushManager.Color = color;
            if (currentPaint != null && currentTool != ToolsPanel.ToolType.Eraser)
            {
                currentPaint.Color = color;
            }

            // ????????? ? ??????? ??????
            colorPicker.AddRecentColor(color);
        }

        public void SetSecondaryColor(SKColor color)
        {
            brushManager.SecondaryColor = color;
            colorPicker.AddRecentColor(color);
        }

        public string GetBrushInfo()
        {
            return string.Format(Localization.Strings.Get("BrushInfo"), (int)brushManager.Size, (int)brushManager.Hardness);
        }

        public SKColor GetCurrentBrushColor()
        {
            return brushManager.GetCurrentColor();
        }

        public SKColor GetSecondaryColor()
        {
            return brushManager.SecondaryColor;
        }

        public ToolsPanel.ToolType GetCurrentTool() => currentTool;
        
        public bool IsOverMoveButton(Point screenPosition)
        {
            if (currentTool != ToolsPanel.ToolType.Selection) return false;
            Point canvasPosition = viewportManager != null ? viewportManager.ScreenToCanvas(screenPosition) : screenPosition;
            return selectionManager.IsMoveButtonHovered(new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y));
        }

        public bool CanUndo => undoRedoManager?.CanUndo ?? false;
        public bool CanRedo => undoRedoManager?.CanRedo ?? false;

        // ????? ?????? ??? ?????
        public void CreateNewLayer(string name = "New Layer")
        {
            layerManager?.CreateNewLayer(name);
        }

        public void RemoveActiveLayer()
        {
            if (layerManager?.ActiveLayerIndex >= 0)
            {
                layerManager.RemoveLayer(layerManager.ActiveLayerIndex);
            }
        }

        public void SetActiveLayer(int index)
        {
            layerManager?.SetActiveLayer(index);
        }

        public void MoveLayerUp(int index)
        {
            layerManager?.MoveLayerUp(index);
        }

        public void MoveLayerDown(int index)
        {
            layerManager?.MoveLayerDown(index);
        }

        public void MergeVisibleLayers()
        {
            layerManager?.MergeAllVisible();
        }

        // ????? ?????? ??? ?????
        public void ToggleGrid()
        {
            if (gridManager != null)
            {
                gridManager.ShowGrid = !gridManager.ShowGrid;
                InvalidateCanvas();
                StatusChanged?.Invoke(gridManager.ShowGrid ? "????? ????????" : "????? ?????????");
            }
        }

        public void ToggleSnapToGrid()
        {
            if (gridManager != null)
            {
                gridManager.SnapToGrid = !gridManager.SnapToGrid;
                StatusChanged?.Invoke(gridManager.SnapToGrid ? "???????? ? ????? ????????" : "???????? ? ????? ?????????");
            }
        }

        public void SetGridSize(float size)
        {
            if (gridManager != null)
            {
                gridManager.GridSize = Math.Max(5, Math.Min(100, size));
                InvalidateCanvas();
            }
        }

        public void AddVerticalGuide(float x)
        {
            gridManager?.AddVerticalGuide(x);
            InvalidateCanvas();
        }

        public void AddHorizontalGuide(float y)
        {
            gridManager?.AddHorizontalGuide(y);
            InvalidateCanvas();
        }

        public void ClearGuides()
        {
            gridManager?.ClearGuides();
            InvalidateCanvas();
        }

        // ????? ?????? ??? ???????
        public List<SKColor> GetColorScheme(ColorSchemeType schemeType)
        {
            return colorPicker?.GetColorScheme(brushManager.PrimaryColor, schemeType) ?? new List<SKColor>();
        }

        public Dictionary<string, List<SKColor>> GetCustomPalettes()
        {
            return colorPicker?.GetCustomPalettes() ?? new Dictionary<string, List<SKColor>>();
        }

        public IEnumerable<SKColor> GetRecentColors()
        {
            return colorPicker?.GetRecentColors() ?? Enumerable.Empty<SKColor>();
        }

        public void AddColorToHistory(SKColor color)
        {
            colorPicker?.AddRecentColor(color);
        }

        public void SaveCustomPalette(string name, List<SKColor> colors)
        {
            colorPicker?.SaveCustomPalette(name, colors);
        }

        public void CreateCustomPalette(string name)
        {
            colorPicker?.CreateCustomPalette(name);
        }

        public void AddColorToPalette(string paletteName, SKColor color)
        {
            colorPicker?.AddColorToPalette(paletteName, color);
        }

        // ??????? ??? UI
        public LayerManager GetLayerManager() => layerManager;
        public GridManager GetGridManager() => gridManager;
        public AdvancedColorPicker GetColorPicker() => colorPicker;
        public UndoRedoManager GetUndoRedoManager() => undoRedoManager;
        public bool IsGridVisible => gridManager?.ShowGrid ?? false;
        public bool IsSnapToGridEnabled => gridManager?.SnapToGrid ?? false;
        public float GridSize => gridManager?.GridSize ?? 20f;

        // ????? ??? ??????????? ??????
        public void InvalidateCanvas()
        {
            needsRedraw = true;
            drawingSurface?.InvalidateVisual();
        }

        private string currentTextInput = "";
        
        public void StartTextInput(Point canvasPosition)
        {
            isTextInputActive = true;
            currentTextInput = "";
            textCursorPosition = new SKPoint((float)Math.Floor(canvasPosition.X), (float)Math.Floor(canvasPosition.Y));
            textCursorVisible = true;
            textCursorTimer.Start();
            InvalidateCanvas();
        }
        
        public void StopTextInput()
        {
            isTextInputActive = false;
            textCursorTimer.Stop();
            InvalidateCanvas();
        }
        
        public bool IsTextInputActive() => isTextInputActive;
        

        
        public void SetCurrentText(string text)
        {
            currentTextInput = text;
            InvalidateCanvas();
        }
        
        public void ApplyText(string font, float size, bool bold, bool italic, bool underline, bool strikethrough)
        {
            if (string.IsNullOrEmpty(currentTextInput)) return;
            
            var position = new Point(textCursorPosition.X, textCursorPosition.Y);
            var color = new SKColor(brushManager.PrimaryColor.Red, brushManager.PrimaryColor.Green, brushManager.PrimaryColor.Blue, currentTextOpacity);
            DrawTextWithFormatting(position, currentTextInput, font, size, color, bold, italic, underline, strikethrough, currentTextAlignment);
            currentTextInput = "";
        }
        
        private void ApplyCurrentText()
        {
            if (string.IsNullOrEmpty(currentTextInput)) return;
            
            var position = new Point(textCursorPosition.X, textCursorPosition.Y);
            TextApplied?.Invoke(position, currentTextInput);
            currentTextInput = "";
        }
        
        private float currentTextSize = 24;
        private string currentTextFont = "Arial";
        private bool currentTextBold = false;
        private bool currentTextItalic = false;
        private bool currentTextUnderline = false;
        private bool currentTextStrikethrough = false;
        
        public void SetTextParameters(string font, float size, bool bold, bool italic, bool underline = false, bool strikethrough = false)
        {
            currentTextFont = font;
            currentTextSize = size;
            currentTextBold = bold;
            currentTextItalic = italic;
            currentTextUnderline = underline;
            currentTextStrikethrough = strikethrough;
        }
        
        private TextAlignment currentTextAlignment = TextAlignment.Left;
        private byte currentTextOpacity = 255;
        
        private string currentGradientStyle = "Linear";
        private List<GradientStop> gradientStops = new List<GradientStop>();
        
        public class GradientStop
        {
            public SKColor Color { get; set; }
            public float Position { get; set; }
        }
        
        public void SetTextAlignment(TextAlignment alignment)
        {
            currentTextAlignment = alignment;
        }
        
        public void SetTextOpacity(byte opacity)
        {
            currentTextOpacity = opacity;
        }
        
        public void SetGradientStyle(string style)
        {
            currentGradientStyle = style;
        }
        
        public void SetGradientStops(List<GradientStop> stops)
        {
            gradientStops = stops;
        }
        
        public List<GradientStop> GetGradientStops() => gradientStops;
        
        public void MoveTextPosition(float dx, float dy)
        {
            textCursorPosition = new SKPoint(textCursorPosition.X + dx, textCursorPosition.Y + dy);
            InvalidateCanvas();
        }
        
        public SKPoint GetTextCursorPosition() => textCursorPosition;
        
        private float GetCurrentTextSize() => currentTextSize;
        
        private SKTypeface GetCurrentTextTypeface()
        {
            var fontStyle = SKFontStyle.Normal;
            if (currentTextBold && currentTextItalic)
                fontStyle = SKFontStyle.BoldItalic;
            else if (currentTextBold)
                fontStyle = SKFontStyle.Bold;
            else if (currentTextItalic)
                fontStyle = SKFontStyle.Italic;
            
            return SKTypeface.FromFamilyName(currentTextFont, fontStyle);
        }
        
        public void ResizeCanvas(int newWidth, int newHeight)
        {
            if (layerManager == null) return;
            layerManager.ResizeAllLayers(newWidth, newHeight);
            InvalidateCanvas();
            StatusChanged?.Invoke($"������ ������: {newWidth}x{newHeight}");
        }

        private void DrawCheckerboard(SKCanvas canvas, SKRect rect)
        {
            const int checkSize = 10;
            using var lightPaint = new SKPaint { Color = new SKColor(255, 255, 255) };
            using var darkPaint = new SKPaint { Color = new SKColor(204, 204, 204) };
            
            for (int y = (int)rect.Top; y < rect.Bottom; y += checkSize)
            {
                for (int x = (int)rect.Left; x < rect.Right; x += checkSize)
                {
                    var paint = ((x / checkSize) + (y / checkSize)) % 2 == 0 ? lightPaint : darkPaint;
                    canvas.DrawRect(x, y, checkSize, checkSize, paint);
                }
            }
        }

        public void Dispose()
        {
            textCursorTimer?.Stop();
            cachedComposite?.Dispose();
            layerManager?.Dispose();
            selectionManager?.Dispose();
            currentPaint?.Dispose();
        }
    }
}









