using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Windows;

namespace mint.paint
{
    public class EnhancedSelectionManager
    {
        private SKRect selectionBounds;
        private SKBitmap clipboardImage;
        private SKBitmap originalSelection;
        private bool isSelecting = false;
        private bool isTransforming = false;
        private SKPoint selectionStart;
        private SKPoint selectionEnd;

        // Трансформации
        private float rotation = 0f;
        private float scaleX = 1f;
        private float scaleY = 1f;
        private SKPoint transformOrigin = new SKPoint(0.5f, 0.5f);

        // Режимы трансформации
        private TransformMode currentTransformMode = TransformMode.None;
        private SKPoint transformStart;

        // Ручки для ресайза
        private Dictionary<ResizeHandle, SKRect> resizeHandles = new Dictionary<ResizeHandle, SKRect>();

        public SKRect? SelectionBounds => HasSelection ? selectionBounds : (SKRect?)null;
        public bool HasSelection => !selectionBounds.IsEmpty;
        public bool HasClipboardImage => clipboardImage != null;
        public float Rotation => rotation;
        public float ScaleX => scaleX;
        public float ScaleY => scaleY;

        public EnhancedSelectionManager()
        {
            InitializeResizeHandles();
        }

        private void InitializeResizeHandles()
        {
            resizeHandles.Clear();
            float handleSize = 8f;

            resizeHandles[ResizeHandle.TopLeft] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.Top] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.TopRight] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.Right] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.BottomRight] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.Bottom] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.BottomLeft] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.Left] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
            resizeHandles[ResizeHandle.Rotate] = new SKRect(-handleSize / 2, -handleSize / 2, handleSize / 2, handleSize / 2);
        }

        public void HandleMouseDown(Point canvasPosition)
        {
            var point = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);
            var transformedPoint = InverseTransformPoint(point);

            // Проверяем клик на ручки трансформации
            if (HasSelection)
            {
                var handle = GetHandleAtPoint(point);
                if (handle != ResizeHandle.None)
                {
                    currentTransformMode = GetTransformModeForHandle(handle);
                    transformStart = point;
                    isTransforming = true;
                    return;
                }

                // Проверяем клик внутри выделения для перемещения
                if (selectionBounds.Contains(transformedPoint))
                {
                    currentTransformMode = TransformMode.Move;
                    transformStart = point;
                    isTransforming = true;
                    return;
                }
            }

            // Начинаем новое выделение
            isSelecting = true;
            selectionStart = point;
            selectionEnd = point;
            selectionBounds = SKRect.Empty;
            ResetTransformations();
        }

        public void HandleMouseMove(Point canvasPosition)
        {
            var point = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);

            if (isTransforming)
            {
                HandleTransformation(point);
            }
            else if (isSelecting)
            {
                selectionEnd = point;
                selectionBounds = new SKRect(
                    Math.Min(selectionStart.X, selectionEnd.X),
                    Math.Min(selectionStart.Y, selectionEnd.Y),
                    Math.Max(selectionStart.X, selectionEnd.X),
                    Math.Max(selectionStart.Y, selectionEnd.Y)
                );
            }
        }

        public void HandleMouseUp(Point canvasPosition)
        {
            if (isSelecting)
            {
                selectionEnd = new SKPoint((float)canvasPosition.X, (float)canvasPosition.Y);
                selectionBounds = new SKRect(
                    Math.Min(selectionStart.X, selectionEnd.X),
                    Math.Min(selectionStart.Y, selectionEnd.Y),
                    Math.Max(selectionStart.X, selectionEnd.X),
                    Math.Max(selectionStart.Y, selectionEnd.Y)
                );

                if (selectionBounds.Width < 5 || selectionBounds.Height < 5)
                {
                    selectionBounds = SKRect.Empty;
                }

                isSelecting = false;
            }

            if (isTransforming)
            {
                isTransforming = false;
                currentTransformMode = TransformMode.None;
            }
        }

        private void HandleTransformation(SKPoint currentPoint)
        {
            var delta = new SKPoint(currentPoint.X - transformStart.X, currentPoint.Y - transformStart.Y);

            switch (currentTransformMode)
            {
                case TransformMode.Move:
                    selectionBounds = new SKRect(
                        selectionBounds.Left + delta.X,
                        selectionBounds.Top + delta.Y,
                        selectionBounds.Right + delta.X,
                        selectionBounds.Bottom + delta.Y
                    );
                    break;

                case TransformMode.ResizeTopLeft:
                    selectionBounds = new SKRect(
                        selectionBounds.Left + delta.X,
                        selectionBounds.Top + delta.Y,
                        selectionBounds.Right,
                        selectionBounds.Bottom
                    );
                    break;

                case TransformMode.ResizeTop:
                    selectionBounds = new SKRect(
                        selectionBounds.Left,
                        selectionBounds.Top + delta.Y,
                        selectionBounds.Right,
                        selectionBounds.Bottom
                    );
                    break;

                case TransformMode.ResizeTopRight:
                    selectionBounds = new SKRect(
                        selectionBounds.Left,
                        selectionBounds.Top + delta.Y,
                        selectionBounds.Right + delta.X,
                        selectionBounds.Bottom
                    );
                    break;

                case TransformMode.ResizeRight:
                    selectionBounds = new SKRect(
                        selectionBounds.Left,
                        selectionBounds.Top,
                        selectionBounds.Right + delta.X,
                        selectionBounds.Bottom
                    );
                    break;

                case TransformMode.ResizeBottomRight:
                    selectionBounds = new SKRect(
                        selectionBounds.Left,
                        selectionBounds.Top,
                        selectionBounds.Right + delta.X,
                        selectionBounds.Bottom + delta.Y
                    );
                    break;

                case TransformMode.ResizeBottom:
                    selectionBounds = new SKRect(
                        selectionBounds.Left,
                        selectionBounds.Top,
                        selectionBounds.Right,
                        selectionBounds.Bottom + delta.Y
                    );
                    break;

                case TransformMode.ResizeBottomLeft:
                    selectionBounds = new SKRect(
                        selectionBounds.Left + delta.X,
                        selectionBounds.Top,
                        selectionBounds.Right,
                        selectionBounds.Bottom + delta.Y
                    );
                    break;

                case TransformMode.ResizeLeft:
                    selectionBounds = new SKRect(
                        selectionBounds.Left + delta.X,
                        selectionBounds.Top,
                        selectionBounds.Right,
                        selectionBounds.Bottom
                    );
                    break;

                case TransformMode.Rotate:
                    var center = new SKPoint(
                        selectionBounds.Left + selectionBounds.Width * transformOrigin.X,
                        selectionBounds.Top + selectionBounds.Height * transformOrigin.Y
                    );

                    var startAngle = Math.Atan2(transformStart.Y - center.Y, transformStart.X - center.X);
                    var currentAngle = Math.Atan2(currentPoint.Y - center.Y, currentPoint.X - center.X);
                    rotation += (float)((currentAngle - startAngle) * 180 / Math.PI);
                    break;
            }

            transformStart = currentPoint;
        }

        private ResizeHandle GetHandleAtPoint(SKPoint point)
        {
            var transformedBounds = GetTransformedBounds();
            var handles = GetResizeHandlePositions(transformedBounds);

            foreach (var handle in handles)
            {
                if (handle.Value.Contains(point))
                    return handle.Key;
            }

            return ResizeHandle.None;
        }

        private Dictionary<ResizeHandle, SKRect> GetResizeHandlePositions(SKRect bounds)
        {
            var positions = new Dictionary<ResizeHandle, SKRect>();
            float handleSize = 8f;
            float halfHandle = handleSize / 2;

            // Угловые ручки
            positions[ResizeHandle.TopLeft] = new SKRect(bounds.Left - halfHandle, bounds.Top - halfHandle, bounds.Left + halfHandle, bounds.Top + halfHandle);
            positions[ResizeHandle.TopRight] = new SKRect(bounds.Right - halfHandle, bounds.Top - halfHandle, bounds.Right + halfHandle, bounds.Top + halfHandle);
            positions[ResizeHandle.BottomRight] = new SKRect(bounds.Right - halfHandle, bounds.Bottom - halfHandle, bounds.Right + halfHandle, bounds.Bottom + halfHandle);
            positions[ResizeHandle.BottomLeft] = new SKRect(bounds.Left - halfHandle, bounds.Bottom - halfHandle, bounds.Left + halfHandle, bounds.Bottom + halfHandle);

            // Боковые ручки
            positions[ResizeHandle.Top] = new SKRect(bounds.MidX - halfHandle, bounds.Top - halfHandle, bounds.MidX + halfHandle, bounds.Top + halfHandle);
            positions[ResizeHandle.Right] = new SKRect(bounds.Right - halfHandle, bounds.MidY - halfHandle, bounds.Right + halfHandle, bounds.MidY + halfHandle);
            positions[ResizeHandle.Bottom] = new SKRect(bounds.MidX - halfHandle, bounds.Bottom - halfHandle, bounds.MidX + halfHandle, bounds.Bottom + halfHandle);
            positions[ResizeHandle.Left] = new SKRect(bounds.Left - halfHandle, bounds.MidY - halfHandle, bounds.Left + halfHandle, bounds.MidY + halfHandle);

            // Ручка поворота
            positions[ResizeHandle.Rotate] = new SKRect(bounds.MidX - halfHandle, bounds.Top - 20 - halfHandle, bounds.MidX + halfHandle, bounds.Top - 20 + halfHandle);

            return positions;
        }

        private TransformMode GetTransformModeForHandle(ResizeHandle handle)
        {
            return handle switch
            {
                ResizeHandle.TopLeft => TransformMode.ResizeTopLeft,
                ResizeHandle.Top => TransformMode.ResizeTop,
                ResizeHandle.TopRight => TransformMode.ResizeTopRight,
                ResizeHandle.Right => TransformMode.ResizeRight,
                ResizeHandle.BottomRight => TransformMode.ResizeBottomRight,
                ResizeHandle.Bottom => TransformMode.ResizeBottom,
                ResizeHandle.BottomLeft => TransformMode.ResizeBottomLeft,
                ResizeHandle.Left => TransformMode.ResizeLeft,
                ResizeHandle.Rotate => TransformMode.Rotate,
                _ => TransformMode.None
            };
        }

        public void DrawSelection(SKCanvas canvas)
        {
            if (!HasSelection) return;

            var transformedBounds = GetTransformedBounds();
            var handles = GetResizeHandlePositions(transformedBounds);

            // Рисуем прямоугольник выделения
            using var selectionPaint = new SKPaint
            {
                Color = new SKColor(0, 120, 215, 50),
                Style = SKPaintStyle.Fill,
                IsAntialias = true
            };

            using var borderPaint = new SKPaint
            {
                Color = new SKColor(0, 120, 215, 255),
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1,
                IsAntialias = true,
                PathEffect = SKPathEffect.CreateDash(new float[] { 3, 3 }, 0)
            };

            canvas.Save();
            ApplyTransformations(canvas);
            canvas.DrawRect(selectionBounds, selectionPaint);
            canvas.DrawRect(selectionBounds, borderPaint);
            canvas.Restore();

            // Рисуем ручки трансформации
            DrawResizeHandles(canvas, handles);
        }

        private void DrawResizeHandles(SKCanvas canvas, Dictionary<ResizeHandle, SKRect> handles)
        {
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
                StrokeWidth = 1,
                IsAntialias = true
            };

            foreach (var handle in handles.Values)
            {
                canvas.DrawRect(handle, handlePaint);
                canvas.DrawRect(handle, handleBorderPaint);
            }
        }

        private SKRect GetTransformedBounds()
        {
            if (!HasSelection) return SKRect.Empty;

            var corners = new[]
            {
                new SKPoint(selectionBounds.Left, selectionBounds.Top),
                new SKPoint(selectionBounds.Right, selectionBounds.Top),
                new SKPoint(selectionBounds.Right, selectionBounds.Bottom),
                new SKPoint(selectionBounds.Left, selectionBounds.Bottom)
            };

            var center = new SKPoint(
                selectionBounds.Left + selectionBounds.Width * transformOrigin.X,
                selectionBounds.Top + selectionBounds.Height * transformOrigin.Y
            );

            // Применяем трансформации к углам
            for (int i = 0; i < corners.Length; i++)
            {
                // Масштабирование
                corners[i] = new SKPoint(
                    center.X + (corners[i].X - center.X) * scaleX,
                    center.Y + (corners[i].Y - center.Y) * scaleY
                );

                // Поворот
                if (rotation != 0)
                {
                    var angle = rotation * Math.PI / 180;
                    var cos = Math.Cos(angle);
                    var sin = Math.Sin(angle);

                    var x = corners[i].X - center.X;
                    var y = corners[i].Y - center.Y;

                    corners[i] = new SKPoint(
                        (float)(center.X + x * cos - y * sin),
                        (float)(center.Y + x * sin + y * cos)
                    );
                }
            }

            // Находим новые границы
            float left = corners[0].X, right = corners[0].X, top = corners[0].Y, bottom = corners[0].Y;
            foreach (var corner in corners)
            {
                left = Math.Min(left, corner.X);
                right = Math.Max(right, corner.X);
                top = Math.Min(top, corner.Y);
                bottom = Math.Max(bottom, corner.Y);
            }

            return new SKRect(left, top, right, bottom);
        }

        private void ApplyTransformations(SKCanvas canvas)
        {
            var center = new SKPoint(
                selectionBounds.Left + selectionBounds.Width * transformOrigin.X,
                selectionBounds.Top + selectionBounds.Height * transformOrigin.Y
            );

            canvas.Translate(center.X, center.Y);
            canvas.RotateDegrees(rotation);
            canvas.Scale(scaleX, scaleY);
            canvas.Translate(-center.X, -center.Y);
        }

        private SKPoint InverseTransformPoint(SKPoint point)
        {
            var center = new SKPoint(
                selectionBounds.Left + selectionBounds.Width * transformOrigin.X,
                selectionBounds.Top + selectionBounds.Height * transformOrigin.Y
            );

            // Обратный поворот
            if (rotation != 0)
            {
                var angle = -rotation * Math.PI / 180;
                var cos = Math.Cos(angle);
                var sin = Math.Sin(angle);

                var x = point.X - center.X;
                var y = point.Y - center.Y;

                point = new SKPoint(
                    (float)(center.X + x * cos - y * sin),
                    (float)(center.Y + x * sin + y * cos)
                );
            }

            // Обратное масштабирование
            point = new SKPoint(
                center.X + (point.X - center.X) / scaleX,
                center.Y + (point.Y - center.Y) / scaleY
            );

            return point;
        }

        public void ResetTransformations()
        {
            rotation = 0f;
            scaleX = 1f;
            scaleY = 1f;
        }

        public void SetRotation(float degrees)
        {
            rotation = degrees % 360;
        }

        public void SetScale(float scale)
        {
            scaleX = scale;
            scaleY = scale;
        }

        public void SetScale(float scaleX, float scaleY)
        {
            this.scaleX = scaleX;
            this.scaleY = scaleY;
        }

        // Остальные методы (Cut, Copy, Paste, Delete) остаются аналогичными предыдущей версии
        public void ClearSelection()
        {
            selectionBounds = SKRect.Empty;
            isSelecting = false;
            isTransforming = false;
            ResetTransformations();
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

                originalSelection = clipboardImage.Copy();
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
                    // Вставляем в центр холста
                    float x = canvasBitmap.Width / 2 - clipboardImage.Width / 2;
                    float y = canvasBitmap.Height / 2 - clipboardImage.Height / 2;

                    var destRect = new SKRect(x, y, x + clipboardImage.Width, y + clipboardImage.Height);
                    canvas.DrawBitmap(clipboardImage, destRect);

                    // Устанавливаем выделение на вставленную область
                    selectionBounds = destRect;
                    ResetTransformations();
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

        public void Dispose()
        {
            clipboardImage?.Dispose();
            originalSelection?.Dispose();
        }
    }

    public enum ResizeHandle
    {
        None,
        TopLeft, Top, TopRight,
        Right, Left,
        BottomLeft, Bottom, BottomRight,
        Rotate
    }

    public enum TransformMode
    {
        None,
        Move,
        ResizeTopLeft, ResizeTop, ResizeTopRight,
        ResizeRight, ResizeLeft,
        ResizeBottomLeft, ResizeBottom, ResizeBottomRight,
        Rotate
    }
}