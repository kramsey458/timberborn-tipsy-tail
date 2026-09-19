using System;
using System.Collections.Generic;

namespace Kyler.TipsyTail
{
    // Own one reference per tile. Synchronous terrain/model callbacks may reenter
    // this object, so update ownership BEFORE notifying the terrain service.
    public sealed class CutoutLease<T> : IDisposable
    {
        private readonly Action<T> _acquire;
        private readonly Action<T> _release;
        private readonly HashSet<T> _owned = new HashSet<T>();
        private HashSet<T> _desired = new HashSet<T>();
        private bool _reconciling;
        private bool _disposed;

        public CutoutLease(Action<T> acquire, Action<T> release)
        {
            _acquire = acquire;
            _release = release;
        }

        public void Show(IEnumerable<T> tiles)
        {
            if (_disposed) return;
            _desired = new HashSet<T>(tiles);
            Reconcile();
        }

        public void Hide()
        {
            _desired.Clear();
            Reconcile();
        }

        public void Dispose()
        {
            _disposed = true;
            Hide();
        }

        private void Reconcile()
        {
            if (_reconciling) return;
            _reconciling = true;
            try
            {
                while (true)
                {
                    if (TryDifference(_owned, _desired, out T oldTile))
                    {
                        _owned.Remove(oldTile);
                        _release(oldTile);
                    }
                    else if (TryDifference(_desired, _owned, out T newTile))
                    {
                        _owned.Add(newTile);
                        _acquire(newTile);
                    }
                    else break;
                }
            }
            finally { _reconciling = false; }
        }

        private static bool TryDifference(HashSet<T> a, HashSet<T> b, out T tile)
        {
            foreach (T item in a)
                if (!b.Contains(item)) { tile = item; return true; }
            tile = default(T);
            return false;
        }
    }
}
