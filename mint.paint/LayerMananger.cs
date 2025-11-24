using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;

namespace mint.paint
{
    public class LayerManager
    {
        private List<Layer> layers = new List<Layer>();

        public IReadOnlyList<Layer> Layers => layers;
        public Layer ActiveLayer { get; private set; }
        public int ActiveLayerIndex => layers.IndexOf(ActiveLayer);
        public bool ShowInactiveLayers { get; set; } = false;
        public float InactiveLayerOpacity { get; set; } = 0.3f;

        public event Action LayersChanged;
        public event Action<Layer> ActiveLayerChanged;

        public LayerManager(int width, int height)
        {
            // Создаем базовый слой
            var baseLayer = new Layer(width, height, "Background");
            baseLayer.Clear();
            AddLayer(baseLayer);
            SetActiveLayer(0);
        }

        public void AddLayer(Layer layer)
        {
            layers.Add(layer);
            LayersChanged?.Invoke();
        }

        public void CreateNewLayer(string name = "New Layer")
        {
            if (layers.Count == 0) return;

            var firstLayer = layers[0];
            var newLayer = new Layer(firstLayer.Bitmap.Width, firstLayer.Bitmap.Height, name);
            AddLayer(newLayer);
            SetActiveLayer(layers.Count - 1);
        }

        public void RemoveLayer(int index)
        {
            if (index < 0 || index >= layers.Count) return;
            if (layers.Count <= 1) return; // Нельзя удалить последний слой

            var layerToRemove = layers[index];
            layers.RemoveAt(index);
            layerToRemove.Dispose();

            // Если удалили активный слой, выбираем следующий
            if (ActiveLayer == layerToRemove)
            {
                SetActiveLayer(Math.Min(index, layers.Count - 1));
            }

            LayersChanged?.Invoke();
        }

        public void SetActiveLayer(int index)
        {
            if (index < 0 || index >= layers.Count) return;

            ActiveLayer = layers[index];
            ActiveLayerChanged?.Invoke(ActiveLayer);
        }

        public void MoveLayerUp(int index)
        {
            if (index <= 0 || index >= layers.Count) return;

            var layer = layers[index];
            layers.RemoveAt(index);
            layers.Insert(index - 1, layer);
            LayersChanged?.Invoke();
        }

        public void MoveLayerDown(int index)
        {
            if (index < 0 || index >= layers.Count - 1) return;

            var layer = layers[index];
            layers.RemoveAt(index);
            layers.Insert(index + 1, layer);
            LayersChanged?.Invoke();
        }

        public void MergeLayers(int bottomIndex, int topIndex)
        {
            if (bottomIndex < 0 || topIndex < 0 ||
                bottomIndex >= layers.Count || topIndex >= layers.Count)
                return;

            var bottomLayer = layers[bottomIndex];
            var topLayer = layers[topIndex];

            using var canvas = new SKCanvas(bottomLayer.Bitmap);
            topLayer.Draw(canvas);

            RemoveLayer(topIndex);
        }

        public void MergeAllVisible()
        {
            var visibleLayers = layers.Where(l => l.IsVisible).ToList();
            if (visibleLayers.Count <= 1) return;

            var baseLayer = visibleLayers[0];
            using var canvas = new SKCanvas(baseLayer.Bitmap);

            for (int i = 1; i < visibleLayers.Count; i++)
            {
                visibleLayers[i].Draw(canvas);
            }

            // Удаляем объединенные слои (кроме базового)
            for (int i = layers.Count - 1; i >= 0; i--)
            {
                if (layers[i] != baseLayer && layers[i].IsVisible)
                {
                    layers[i].Dispose();
                    layers.RemoveAt(i);
                }
            }

            LayersChanged?.Invoke();
        }

        public void DrawAllLayers(SKCanvas canvas, double zoomLevel = 1.0)
        {
            var paint = zoomLevel > 4.0 ? new SKPaint { FilterQuality = SKFilterQuality.None } : null;
            foreach (var layer in layers)
            {
                if (layer.IsVisible || ShowInactiveLayers)
                {
                    layer.Draw(canvas, paint, ShowInactiveLayers, InactiveLayerOpacity);
                }
            }
            paint?.Dispose();
        }

        public void ResizeAllLayers(int newWidth, int newHeight)
        {
            foreach (var layer in layers)
            {
                layer.Resize(newWidth, newHeight);
            }
            LayersChanged?.Invoke();
        }

        public void Dispose()
        {
            foreach (var layer in layers)
            {
                layer.Dispose();
            }
            layers.Clear();
        }
    }
}