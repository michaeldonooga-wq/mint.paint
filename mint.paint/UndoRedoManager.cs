using SkiaSharp;
using System.Collections.Generic;

namespace mint.paint
{
    public class UndoRedoManager
    {
        private Stack<SKBitmap> undoStack;
        private Stack<SKBitmap> redoStack;
        private int maxUndoSteps;

        public int MaxUndoSteps
        {
            get => maxUndoSteps;
            set => maxUndoSteps = System.Math.Max(1, System.Math.Min(value, 50));
        }

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
        public int UndoCount => undoStack.Count;
        public int RedoCount => redoStack.Count;

        public UndoRedoManager()
        {
            undoStack = new Stack<SKBitmap>();
            redoStack = new Stack<SKBitmap>();
            maxUndoSteps = GetOptimalUndoSteps();
        }

        private int GetOptimalUndoSteps()
        {
            var totalMemoryGB = new Microsoft.VisualBasic.Devices.ComputerInfo().TotalPhysicalMemory / (1024.0 * 1024.0 * 1024.0);
            if (totalMemoryGB >= 16) return 30;
            if (totalMemoryGB >= 8) return 20;
            if (totalMemoryGB >= 4) return 10;
            return 5;
        }

        public void SaveState(SKBitmap bitmap)
        {
            var snapshot = CreateBitmapSnapshot(bitmap);
            undoStack.Push(snapshot);
            
            foreach (var bmp in redoStack)
                bmp?.Dispose();
            redoStack.Clear();

            if (undoStack.Count > maxUndoSteps)
            {
                var tempStack = new Stack<SKBitmap>();
                for (int i = 0; i < maxUndoSteps; i++)
                {
                    tempStack.Push(undoStack.Pop());
                }
                while (undoStack.Count > 0)
                {
                    undoStack.Pop()?.Dispose();
                }
                undoStack = tempStack;
            }
        }

        public SKBitmap Undo(SKBitmap currentBitmap)
        {
            if (!CanUndo) return currentBitmap;

            var currentState = CreateBitmapSnapshot(currentBitmap);
            redoStack.Push(currentState);
            return undoStack.Pop();
        }

        public SKBitmap Redo(SKBitmap currentBitmap)
        {
            if (!CanRedo) return currentBitmap;

            var currentState = CreateBitmapSnapshot(currentBitmap);
            undoStack.Push(currentState);
            return redoStack.Pop();
        }

        private SKBitmap CreateBitmapSnapshot(SKBitmap source)
        {
            var snapshot = new SKBitmap(source.Width, source.Height);
            using (var canvas = new SKCanvas(snapshot))
            {
                canvas.DrawBitmap(source, 0, 0);
            }
            return snapshot;
        }

        public void Clear()
        {
            foreach (var bmp in undoStack)
                bmp?.Dispose();
            foreach (var bmp in redoStack)
                bmp?.Dispose();
            undoStack.Clear();
            redoStack.Clear();
        }
    }
}