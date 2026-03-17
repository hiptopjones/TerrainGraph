using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace CodeFirst.TerrainGraph.Editor
{
    [InitializeOnLoad]
    public static class TextureMemoryManager
    {
        private static readonly List<(WeakReference, Texture)> _entries = new();

        static TextureMemoryManager()
        {
            EditorApplication.update += Cleanup;
        }

        // Register an object and its texture for automatic cleanup when object is GC'ed.
        public static void Register(object obj, Texture texture)
        {
            if (obj == null || texture == null)
            {
                return;
            }

            _entries.Add((new WeakReference(obj), texture));
        }

        public static void Unregister(Texture texture)
        {
            var entry = _entries.SingleOrDefault(x => x.Item2 == texture);

            var index = _entries.IndexOf(entry);
            if (index >= 0)
            {
                // Swap and pop
                var lastIndex = _entries.Count - 1;
                _entries[index] = _entries[lastIndex];
                _entries.RemoveAt(lastIndex);
            }
        }

        private static void Cleanup()
        {
            for (int i = _entries.Count - 1; i >= 0; i--)
            {
                var (weakReference, texture) = _entries[i];

                if (!weakReference.IsAlive)
                {
                    // Object has been GC'd
                    if (texture != null)
                    {
                        UnityObject.DestroyImmediate(texture);
                    }

                    _entries.RemoveAt(i);
                }
            }
        }
    }
}
