using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using AOT;

namespace UniBridge {
    // メモリアロケーター
    class GlobalAllocator {
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate IntPtr AllocMemoryDelegate(UIntPtr size);
        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        delegate void DeallocMemoryDelegate(IntPtr pointer);
        
        // メモリ確保と解放
        [MonoPInvokeCallback(typeof(AllocMemoryDelegate))]
        static IntPtr AllocMemory(UIntPtr size) {
            return Marshal.AllocHGlobal((int) size);
        }
        
        [MonoPInvokeCallback(typeof(DeallocMemoryDelegate))]
        static void DeallocMemory(IntPtr pointer) {
            Marshal.FreeHGlobal(pointer);
        }
    }

    // Rustのスライス
    [StructLayout(LayoutKind.Sequential)]
    public struct Slice<T> where T : unmanaged {
        private readonly unsafe T*      head;
        private readonly UIntPtr len;

        public int Length => (int) len;
        
        private unsafe T* GetPointer(int index) {
            // 領域の検査
            if (index < 0 || Length <= index) {
                throw new IndexOutOfRangeException($"array index is out of range: index is {index} but len is {len}");
            }

            // 対応する位置のポインタを渡す
            return &head[index];
        }

        private void SetValue(int index, T value) {
            unsafe {
                var entry = GetPointer(index);
                *entry = value;
            }
        }

        // index operator
        public T this[int index] {
            set => SetValue(index, value);
            get {
                unsafe {
                    return *GetPointer(index);
                }
            }
        }

        public static unsafe implicit operator T*(Slice<T> self) => self.head;
        
        public IEnumerator<T> GetEnumerator() {
            for (var i = 0; i < Length; i++)
                yield return this[i];
        }
    }
}
