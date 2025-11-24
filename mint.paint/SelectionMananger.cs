using SkiaSharp;
using System;
using System.Windows;

namespace mint.paint
{
    public enum SelectionType
    {
        Rectangle,
        Ellipse,
        Lasso
    }
    
    public class SelectionManager
    {
        private SKRect selectionBounds;
        private SKBitmap clipboardImage;
        private SKBitmap selectionContent;
        private bool isSelecting = false;
        private bool isMoving = false;
        private bool isResizing = false;
        private bool isRotating = false;
        private SKPoint selectionStart;
        private SKPoint selectionEnd;
        private SKPoint dragStart;
        private int resizeHandle = -1;
        private SKBitmap sourceBitmap;
        private float rotationAngle = 0;
        private SKRect originalBounds;
        private SelectionType selectionType = SelectionType.Rectangle;
        private List<SKPoint> lassoPoints = new List<SKPoint>();

        public SKRect? SelectionBounds => HasSelection ? selectionBounds : (SKRect?)null;
        public bool HasSelection => !selectionBounds.IsEmpty;
        public bool HasClipboardImage => clipboardImage != null;
        public SelectionType CurrentSelectionType => selectionType;
        public List<SKPoint> LassoPoints => lassoPoints;
        
        public void SetSelectionType(SelectionType type)
        {
            selectionType = type;
        }

        public void HandleMouseDown(Point canvasPosition, bool isRightButton = false)
        {
            var point = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);

            if (sourceBitmap == null) return;

            // ПКМ внутри выделения - поворот
            if (isRightButton && selectionBounds.Contains(point))
            {
                if (selectionContent == null)
                {
                    CaptureSelectionContent();
                    originalBounds = selectionBounds;
                }
                isRotating = true;
                dragStart = point;
                return;
            }

            // Проверяем клик на ручки ресайза
            resizeHandle = GetHandleAtPoint(point);
            if (resizeHandle >= 0)
            {
                isResizing = true;
                dragStart = point;
                return;
            }
            
            // Проверяем клик внутри выделения - перемещение
            if (selectionBounds.Contains(point))
            {
                isMoving = true;
                dragStart = point;
                return;
            }

            // Клик вне выделения - ничего не делаем если выделение уже есть
            if (!HasSelection)
            {
                isSelecting = true;
                selectionStart = new SKPoint((float)Math.Floor(point.X), (float)Math.Floor(point.Y));
                selectionEnd = selectionStart;
                if (selectionType == SelectionType.Lasso)
                {
                    lassoPoints.Clear();
                    lassoPoints.Add(selectionStart);
                }
            }
        }

        public void HandleMouseMove(Point canvasPosition)
        {
            var point = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);
            
            if (!isSelecting && !isMoving && !isResizing && !isRotating)
            {
                hoveredHandle = GetHandleAtPoint(point);
                var center = new SKPoint(selectionBounds.MidX, selectionBounds.MidY);
                float dist = (float)Math.Sqrt(Math.Pow(point.X - center.X, 2) + Math.Pow(point.Y - center.Y, 2));
                isMoveButtonHovered = dist < (15 / currentZoom);
            }

            if (sourceBitmap == null) return;

            if (isRotating && sourceBitmap != null)
            {
                var center = new SKPoint(selectionBounds.MidX, selectionBounds.MidY);
                var startVector = new SKPoint(dragStart.X - center.X, dragStart.Y - center.Y);
                var currentVector = new SKPoint(point.X - center.X, point.Y - center.Y);
                
                float angle1 = (float)Math.Atan2(startVector.Y, startVector.X);
                float angle2 = (float)Math.Atan2(currentVector.Y, currentVector.X);
                float deltaAngle = angle2 - angle1;
                
                rotationAngle += deltaAngle * 180f / (float)Math.PI;
                dragStart = point;
                return;
            }

            if (isResizing && sourceBitmap != null)
            {
                if (selectionContent == null)
                {
                    CaptureSelectionContent();
                    originalBounds = selectionBounds;
                }
                ResizeSelection(point);
                return;
            }

            if (isMoving && sourceBitmap != null)
            {
                if (selectionContent == null)
                {
                    CaptureSelectionContent();
                }
                float dx = point.X - dragStart.X;
                float dy = point.Y - dragStart.Y;
                selectionBounds = new SKRect(
                    selectionBounds.Left + dx,
                    selectionBounds.Top + dy,
                    selectionBounds.Right + dx,
                    selectionBounds.Bottom + dy
                );
                dragStart = point;
                return;
            }

            if (isSelecting)
            {
                selectionEnd = new SKPoint((float)Math.Floor(point.X), (float)Math.Floor(point.Y));
                if (selectionType == SelectionType.Lasso)
                {
                    lassoPoints.Add(selectionEnd);
                    if (lassoPoints.Count > 1)
                    {
                        float minX = lassoPoints[0].X, maxX = lassoPoints[0].X;
                        float minY = lassoPoints[0].Y, maxY = lassoPoints[0].Y;
                        foreach (var p in lassoPoints)
                        {
                            if (p.X < minX) minX = p.X;
                            if (p.X > maxX) maxX = p.X;
                            if (p.Y < minY) minY = p.Y;
                            if (p.Y > maxY) maxY = p.Y;
                        }
                        selectionBounds = new SKRect((float)Math.Floor(minX), (float)Math.Floor(minY), (float)Math.Ceiling(maxX), (float)Math.Ceiling(maxY));
                    }
                }
                else
                {
                    selectionBounds = new SKRect(
                        (float)Math.Floor(Math.Min(selectionStart.X, selectionEnd.X)),
                        (float)Math.Floor(Math.Min(selectionStart.Y, selectionEnd.Y)),
                        (float)Math.Ceiling(Math.Max(selectionStart.X, selectionEnd.X)),
                        (float)Math.Ceiling(Math.Max(selectionStart.Y, selectionEnd.Y))
                    );
                }
            }
        }

        public void HandleMouseUp(Point canvasPosition)
        {
            if (isSelecting)
            {
                selectionEnd = new SKPoint((float)Math.Floor(canvasPosition.X), (float)Math.Floor(canvasPosition.Y));
                selectionBounds = new SKRect(
                    (float)Math.Floor(Math.Min(selectionStart.X, selectionEnd.X)),
                    (float)Math.Floor(Math.Min(selectionStart.Y, selectionEnd.Y)),
                    (float)Math.Ceiling(Math.Max(selectionStart.X, selectionEnd.X)),
                    (float)Math.Ceiling(Math.Max(selectionStart.Y, selectionEnd.Y))
                );

                if (selectionType != SelectionType.Lasso && (selectionBounds.Width < 5 || selectionBounds.Height < 5))
                {
                    selectionBounds = SKRect.Empty;
                }
                else
                {
                    handleScale = 0;
                    moveButtonScale = 0;
                }
            }

            isSelecting = false;
            isMoving = false;
            isResizing = false;
            isRotating = false;
            resizeHandle = -1;
        }

        private void CaptureSelectionContent()
        {
            if (!HasSelection || sourceBitmap == null || selectionContent != null) return;

            try
            {
                var rect = new SKRectI(
                    (int)selectionBounds.Left,
                    (int)selectionBounds.Top,
                    (int)selectionBounds.Right,
                    (int)selectionBounds.Bottom
                );

                selectionContent?.Dispose();
                selectionContent = new SKBitmap(rect.Width, rect.Height);
                using (var canvas = new SKCanvas(selectionContent))
                {
                    var destRect = new SKRect(0, 0, rect.Width, rect.Height);
                    canvas.DrawBitmap(sourceBitmap, rect, destRect);
                }

                using (var canvas = new SKCanvas(sourceBitmap))
                using (var clearPaint = new SKPaint { Color = SKColors.White })
                {
                    canvas.DrawRect(selectionBounds, clearPaint);
                }
            }
            catch { }
        }

        private void ApplySelection()
        {
            if (selectionContent != null && sourceBitmap != null && HasSelection)
            {
                using (var canvas = new SKCanvas(sourceBitmap))
                using (var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High })
                {
                    canvas.Save();
                    canvas.Translate(selectionBounds.MidX, selectionBounds.MidY);
                    canvas.RotateDegrees(rotationAngle);
                    
                    var destRect = new SKRect(
                        -selectionBounds.Width / 2f,
                        -selectionBounds.Height / 2f,
                        selectionBounds.Width / 2f,
                        selectionBounds.Height / 2f
                    );
                    canvas.DrawBitmap(selectionContent, destRect, paint);
                    canvas.Restore();
                }
                selectionContent?.Dispose();
                selectionContent = null;
                rotationAngle = 0;
                originalBounds = SKRect.Empty;
            }
        }

        public void SetSourceBitmap(SKBitmap bitmap)
        {
            if (sourceBitmap != bitmap && sourceBitmap != null)
            {
                ApplySelection();
            }
            sourceBitmap = bitmap;
        }

        public void SetSelection(SKRect rect)
        {
            ApplySelection();
            selectionBounds = rect;
            selectionContent = null;
            rotationAngle = 0;
            handleScale = 0;
            for (int i = 0; i < handleHoverScale.Length; i++) handleHoverScale[i] = 1f;
        }



        public void DrawSelection(SKCanvas canvas, float zoom = 1f, bool showHandles = false)
        {
            if (!HasSelection) return;

            // Рисуем содержимое выделения
            if (selectionContent != null)
            {
                canvas.Save();
                canvas.Translate(selectionBounds.MidX, selectionBounds.MidY);
                canvas.RotateDegrees(rotationAngle);
                
                var destRect = new SKRect(
                    -selectionBounds.Width / 2f,
                    -selectionBounds.Height / 2f,
                    selectionBounds.Width / 2f,
                    selectionBounds.Height / 2f
                );
                canvas.DrawBitmap(selectionContent, destRect);
                canvas.Restore();
            }

            using var borderPaint = new SKPaint
            {
                Color = new SKColor(0, 120, 215, 255),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1 / zoom,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 3 / zoom, 3 / zoom }, 0)
            };

            if (selectionType == SelectionType.Ellipse)
            {
                canvas.DrawOval(selectionBounds, borderPaint);
            }
            else if (selectionType == SelectionType.Lasso && lassoPoints.Count > 1)
            {
                using var path = new SKPath();
                path.MoveTo(lassoPoints[0]);
                for (int i = 1; i < lassoPoints.Count; i++)
                {
                    path.LineTo(lassoPoints[i]);
                }
                path.Close();
                canvas.DrawPath(path, borderPaint);
            }
            else
            {
                canvas.DrawRect(selectionBounds, borderPaint);
            }
            
            if (showHandles)
            {
                DrawResizeHandles(canvas, zoom);
                DrawMoveButton(canvas, zoom);
            }
        }
        
        private float moveButtonScale = 0;
        private float moveButtonHoverScale = 1f;
        private bool isMoveButtonHovered = false;
        
        private void DrawMoveButton(SKCanvas canvas, float zoom)
        {
            if (moveButtonScale < 1) moveButtonScale += 0.15f;
            if (moveButtonScale > 1) moveButtonScale = 1;
            
            float targetHoverScale = isMoveButtonHovered ? 1.2f : 1f;
            if (moveButtonHoverScale < targetHoverScale) moveButtonHoverScale += 0.05f;
            if (moveButtonHoverScale > targetHoverScale) moveButtonHoverScale -= 0.05f;
            if (Math.Abs(moveButtonHoverScale - targetHoverScale) < 0.02f) moveButtonHoverScale = targetHoverScale;
            
            float size = 24 / zoom * moveButtonScale * moveButtonHoverScale;
            var center = new SKPoint(selectionBounds.MidX, selectionBounds.MidY);
            
            using var bgPaint = new SKPaint { Color = SKColors.White, IsAntialias = true };
            using var borderPaint = new SKPaint { Color = new SKColor(0, 120, 215), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2f / zoom };
            using var iconPaint = new SKPaint { Color = SKColors.White, IsAntialias = true, StrokeWidth = 1.5f / zoom, Style = SKPaintStyle.Stroke };
            
            canvas.DrawCircle(center, size/2, bgPaint);
            canvas.DrawCircle(center, size/2, borderPaint);
            
            using var arrowPaint = new SKPaint { Color = new SKColor(0, 120, 215), IsAntialias = true, StrokeWidth = 1.5f / zoom, Style = SKPaintStyle.Stroke };
            float arrowSize = size * 0.3f;
            float arrowTip = 3 / zoom;
            canvas.DrawLine(center.X - arrowSize, center.Y, center.X + arrowSize, center.Y, arrowPaint);
            canvas.DrawLine(center.X, center.Y - arrowSize, center.X, center.Y + arrowSize, arrowPaint);
            canvas.DrawLine(center.X - arrowSize, center.Y, center.X - arrowSize + arrowTip, center.Y - arrowTip, arrowPaint);
            canvas.DrawLine(center.X - arrowSize, center.Y, center.X - arrowSize + arrowTip, center.Y + arrowTip, arrowPaint);
            canvas.DrawLine(center.X + arrowSize, center.Y, center.X + arrowSize - arrowTip, center.Y - arrowTip, arrowPaint);
            canvas.DrawLine(center.X + arrowSize, center.Y, center.X + arrowSize - arrowTip, center.Y + arrowTip, arrowPaint);
            canvas.DrawLine(center.X, center.Y - arrowSize, center.X - arrowTip, center.Y - arrowSize + arrowTip, arrowPaint);
            canvas.DrawLine(center.X, center.Y - arrowSize, center.X + arrowTip, center.Y - arrowSize + arrowTip, arrowPaint);
            canvas.DrawLine(center.X, center.Y + arrowSize, center.X - arrowTip, center.Y + arrowSize - arrowTip, arrowPaint);
            canvas.DrawLine(center.X, center.Y + arrowSize, center.X + arrowTip, center.Y + arrowSize - arrowTip, arrowPaint);
        }

        private float handleScale = 0;
        private int hoveredHandle = -1;
        private float[] handleHoverScale = new float[8];
        
        private void DrawResizeHandles(SKCanvas canvas, float zoom)
        {
            if (handleScale < 1) handleScale += 0.15f;
            if (handleScale > 1) handleScale = 1;
            
            using var handlePaint = new SKPaint
            {
                Color = SKColors.White,
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var handleBorderPaint = new SKPaint
            {
                Color = new SKColor(0, 120, 215, 255),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1 / zoom,
                IsAntialias = true
            };

            float baseHandleSize = 6 / zoom * handleScale;
            var handles = new[]
            {
                new SKPoint(selectionBounds.Left, selectionBounds.Top), // NW
                new SKPoint(selectionBounds.MidX, selectionBounds.Top), // N
                new SKPoint(selectionBounds.Right, selectionBounds.Top), // NE
                new SKPoint(selectionBounds.Right, selectionBounds.MidY), // E
                new SKPoint(selectionBounds.Right, selectionBounds.Bottom), // SE
                new SKPoint(selectionBounds.MidX, selectionBounds.Bottom), // S
                new SKPoint(selectionBounds.Left, selectionBounds.Bottom), // SW
                new SKPoint(selectionBounds.Left, selectionBounds.MidY) // W
            };

            for (int i = 0; i < handles.Length; i++)
            {
                float targetScale = i == hoveredHandle ? 1.5f : 1f;
                if (handleHoverScale[i] < targetScale) handleHoverScale[i] += 0.1f;
                if (handleHoverScale[i] > targetScale) handleHoverScale[i] -= 0.1f;
                if (Math.Abs(handleHoverScale[i] - targetScale) < 0.05f) handleHoverScale[i] = targetScale;
                
                float handleSize = baseHandleSize * handleHoverScale[i];
                var handleRect = new SKRect(
                    handles[i].X - handleSize / 2,
                    handles[i].Y - handleSize / 2,
                    handles[i].X + handleSize / 2,
                    handles[i].Y + handleSize / 2
                );

                canvas.DrawRect(handleRect, handlePaint);
                canvas.DrawRect(handleRect, handleBorderPaint);
            }
        }

        private float currentZoom = 1f;
        
        public void SetZoom(float zoom)
        {
            currentZoom = zoom;
        }
        
        private int GetHandleAtPoint(SKPoint point)
        {
            if (!HasSelection) return -1;

            float handleSize = 12 / Math.Max(0.1f, currentZoom);
            var handles = new[]
            {
                new SKPoint(selectionBounds.Left, selectionBounds.Top),
                new SKPoint(selectionBounds.MidX, selectionBounds.Top),
                new SKPoint(selectionBounds.Right, selectionBounds.Top),
                new SKPoint(selectionBounds.Right, selectionBounds.MidY),
                new SKPoint(selectionBounds.Right, selectionBounds.Bottom),
                new SKPoint(selectionBounds.MidX, selectionBounds.Bottom),
                new SKPoint(selectionBounds.Left, selectionBounds.Bottom),
                new SKPoint(selectionBounds.Left, selectionBounds.MidY)
            };

            for (int i = handles.Length - 1; i >= 0; i--)
            {
                float dx = point.X - handles[i].X;
                float dy = point.Y - handles[i].Y;
                float dist = (float)Math.Sqrt(dx * dx + dy * dy);
                
                if (dist < handleSize)
                    return i;
            }

            return -1;
        }

        private void ResizeSelection(SKPoint point)
        {
            var left = selectionBounds.Left;
            var top = selectionBounds.Top;
            var right = selectionBounds.Right;
            var bottom = selectionBounds.Bottom;

            switch (resizeHandle)
            {
                case 0: left = point.X; top = point.Y; break;
                case 1: top = point.Y; break;
                case 2: right = point.X; top = point.Y; break;
                case 3: right = point.X; break;
                case 4: right = point.X; bottom = point.Y; break;
                case 5: bottom = point.Y; break;
                case 6: left = point.X; bottom = point.Y; break;
                case 7: left = point.X; break;
            }

            selectionBounds = new SKRect(
                Math.Min(left, right),
                Math.Min(top, bottom),
                Math.Max(left, right),
                Math.Max(top, bottom)
            );
        }

        public void ApplyAndClearSelection()
        {
            ApplySelection();
            selectionBounds = SKRect.Empty;
            lassoPoints.Clear();
            isSelecting = false;
            isMoving = false;
            isResizing = false;
            isRotating = false;
            rotationAngle = 0;
        }
        
        public void ClearSelection()
        {
            selectionBounds = SKRect.Empty;
            lassoPoints.Clear();
            selectionContent?.Dispose();
            selectionContent = null;
            isSelecting = false;
            isMoving = false;
            isResizing = false;
            isRotating = false;
            rotationAngle = 0;
        }

        public void CutSelection(SKBitmap canvasBitmap)
        {
            if (!HasSelection) return;

            CopySelection(canvasBitmap);
            DeleteSelection(canvasBitmap);
        }

        public void CopySelection(SKBitmap canvasBitmap)
        {
            if (!HasSelection) return;

            try
            {
                // Создаем bitmap из выделенной области
                var selectionRect = new SKRectI(
                    (int)selectionBounds.Left,
                    (int)selectionBounds.Top,
                    (int)selectionBounds.Right,
                    (int)selectionBounds.Bottom
                );

                clipboardImage = new SKBitmap(selectionRect.Width, selectionRect.Height);
                using (var canvas = new SKCanvas(clipboardImage))
                {
                    var sourceRect = selectionRect;
                    var destRect = new SKRect(0, 0, selectionRect.Width, selectionRect.Height);
                    canvas.DrawBitmap(canvasBitmap, sourceRect, destRect);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Copy selection error: {ex.Message}");
            }
        }

        public void PasteSelection(SKBitmap canvasBitmap)
        {
            if (!HasClipboardImage) return;

            try
            {
                using (var canvas = new SKCanvas(canvasBitmap))
                {
                    // Вставляем в центр холста или в последнюю позицию
                    float x = canvasBitmap.Width / 2 - clipboardImage.Width / 2;
                    float y = canvasBitmap.Height / 2 - clipboardImage.Height / 2;

                    var destRect = new SKRect(x, y, x + clipboardImage.Width, y + clipboardImage.Height);
                    canvas.DrawBitmap(clipboardImage, destRect);

                    // Устанавливаем выделение на вставленную область
                    selectionBounds = destRect;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Paste selection error: {ex.Message}");
            }
        }

        public void DeleteSelection(SKBitmap canvasBitmap)
        {
            if (!HasSelection) return;

            try
            {
                using (var canvas = new SKCanvas(canvasBitmap))
                using (var clearPaint = new SKPaint { Color = SKColors.White })
                {
                    canvas.DrawRect(selectionBounds, clearPaint);
                }

                ClearSelection();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Delete selection error: {ex.Message}");
            }
        }

        public bool IsMoveButtonHovered(SKPoint point)
        {
            if (!HasSelection) return false;
            var center = new SKPoint(selectionBounds.MidX, selectionBounds.MidY);
            float dist = (float)Math.Sqrt(Math.Pow(point.X - center.X, 2) + Math.Pow(point.Y - center.Y, 2));
            return dist < (15 / currentZoom);
        }
        
        public bool IsPointInSelection(SKPoint point)
        {
            if (!HasSelection) return true;
            
            if (selectionType == SelectionType.Ellipse)
            {
                var cx = selectionBounds.MidX;
                var cy = selectionBounds.MidY;
                var rx = selectionBounds.Width / 2;
                var ry = selectionBounds.Height / 2;
                var dx = (point.X - cx) / rx;
                var dy = (point.Y - cy) / ry;
                return (dx * dx + dy * dy) <= 1;
            }
            else if (selectionType == SelectionType.Lasso && lassoPoints.Count > 2)
            {
                int intersections = 0;
                for (int i = 0; i < lassoPoints.Count; i++)
                {
                    var p1 = lassoPoints[i];
                    var p2 = lassoPoints[(i + 1) % lassoPoints.Count];
                    if ((p1.Y > point.Y) != (p2.Y > point.Y))
                    {
                        var slope = (point.Y - p1.Y) / (p2.Y - p1.Y);
                        if (point.X < p1.X + slope * (p2.X - p1.X))
                        {
                            intersections++;
                        }
                    }
                }
                return (intersections % 2) == 1;
            }
            else
            {
                return selectionBounds.Contains(point);
            }
        }
        
        public void Dispose()
        {
            clipboardImage?.Dispose();
            selectionContent?.Dispose();
        }
    }
}