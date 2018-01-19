using System;
using System.Collections.Generic;
using System.Threading;

namespace BrainCommon
{
    public struct DisposableCollector : IDisposable
    {
        private List<IDisposable> _lst;
        private volatile bool _isDisposed;

        public static DisposableCollector operator +(DisposableCollector thiz, IDisposable added)
        {
            thiz.Add(added);
            return thiz;
        }

        private void Add(IDisposable added)
        {
            if (_isDisposed)
                throw new ObjectDisposedException("object has been disposed");
            
            if (_lst == null)
                _lst=new List<IDisposable>();
            
            _lst.Add(added);
        }

        public void Dispose()
        {
            _isDisposed = true;
            var local = Interlocked.Exchange(ref _lst, null);
            if (local == null) return;
            for(var i = 0; i < local.Count; i++)
            {
                local[i].Dispose();
            }
        }
    }
}
