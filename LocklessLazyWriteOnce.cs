using System;
using System.Diagnostics;
using System.Threading;
using JetBrains.Annotations;

namespace HpTimesStamps
{
    internal class LocklessLazyWriteOnce<T> where T : class
    {
        public bool IsSet => _value != null;
        
        [NotNull]
        public T Value
        {
            get
            {
                T ret = _value;
                if (ret == null)
                {
                    try
                    {
                        T temp = _initializer();
                        if (temp == null)
                        {
                            throw new DelegateReturnedNullException(nameof(_initializer), _initializer);
                        }

                        Interlocked.CompareExchange(ref _value, temp, null);
                    }
                    catch (DelegateException)
                    {
                        throw;
                    }
                    catch (Exception inner)
                    {
                        throw new DelegateThrewException(nameof(_initializer), _initializer, inner);
                    }

                    ret = _value;
                }
                Debug.Assert(ret != null);
                return ret;
            }
        }

        public bool TrySetNotDefaultValue([NotNull] T nonDefaultValue)
        {
            if (nonDefaultValue == null) throw new ArgumentNullException(nameof(nonDefaultValue));
            return Interlocked.CompareExchange(ref _value, nonDefaultValue, null) == null;
        }

        public void SetNotDefaultValueOrThrow([NotNull] T nonDefaultValue)
        {
            if (!TrySetNotDefaultValue(nonDefaultValue))
            {
                throw new InvalidOperationException("The value has already been set.");
            }
        }

        public LocklessLazyWriteOnce([NotNull] Func<T> initializer) =>
            _initializer = initializer?? throw new ArgumentNullException(nameof(initializer));
        

        private readonly Func<T> _initializer;
        [CanBeNull] private volatile T _value;
    }
}