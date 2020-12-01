using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Leopotam.SimpleBinary {
    public sealed class ListPool<T> {
        List<T>[] _pool;
        int _poolCount;

        public ListPool (int capacity = 8) {
            _pool = new List<T>[capacity];
            _poolCount = 0;
        }

        public List<T> Get () {
            if (_poolCount > 0) {
                return _pool[--_poolCount];
            }
            return new List<T> ();
        }

        public void Recycle (List<T> item) {
            item.Clear ();
            if (_pool.Length == _poolCount) {
                Array.Resize (ref _pool, _poolCount << 1);
            }
            _pool[_poolCount++] = item;
        }
    }
    
    public struct SimpleBinarySerializer {
        byte[] _data;
        int _offset;

        public SimpleBinarySerializer (byte[] data, int offset = 0) {
            _data = data;
            _offset = offset;
        }

        void CheckCapacity (int space) {
            if (_data.Length < _offset + space) {
                Array.Resize (ref _data, _data.Length << 1);
            }
        }

        public ArraySegment<byte> GetBuffer () {
            return new ArraySegment<byte> (_data, 0, _offset);
        }

        public ushort PeekPacketType () {
            return (ushort) (_data[_offset] | (uint) _data[_offset + 1] << 8);
        }

        public byte ReadU8 () {
            var v = _data[_offset];
            _offset++;
            return v;
        }

        public sbyte ReadI8 () {
            var v = (sbyte) _data[_offset];
            _offset++;
            return v;
        }

        public void WriteU8 (byte v) {
            CheckCapacity (1);
            _data[_offset] = v;
            _offset++;
        }

        public void WriteI8 (sbyte v) {
            CheckCapacity (1);
            _data[_offset] = (byte) v;
            _offset++;
        }

        public ushort ReadU16 () {
            var v = (ushort) (_data[_offset] | (uint) _data[_offset + 1] << 8);
            _offset += 2;
            return v;
        }

        public short ReadI16 () {
            var v = (short) (_data[_offset] | _data[_offset + 1] << 8);
            _offset += 2;
            return v;
        }

        public void WriteU16 (ushort v) {
            CheckCapacity (2);
            _data[_offset] = (byte) v;
            _data[_offset + 1] = (byte) ((uint) v >> 8);
            _offset += 2;
        }

        public void WriteI16 (short v) {
            CheckCapacity (2);
            _data[_offset] = (byte) v;
            _data[_offset + 1] = (byte) ((uint) v >> 8);
            _offset += 2;
        }

        public uint ReadU32 () {
            var v = (uint) (
                _data[_offset]
                | _data[_offset + 1] << 8
                | _data[_offset + 2] << 16
                | _data[_offset + 3] << 24);
            _offset += 4;
            return v;
        }

        public int ReadI32 () {
            var v =
                _data[_offset]
                | _data[_offset + 1] << 8
                | _data[_offset + 2] << 16
                | _data[_offset + 3] << 24;
            _offset += 4;
            return v;
        }

        public void WriteU32 (uint v) {
            CheckCapacity (4);
            _data[_offset] = (byte) v;
            _data[_offset + 1] = (byte) (v >> 8);
            _data[_offset + 2] = (byte) (v >> 16);
            _data[_offset + 3] = (byte) (v >> 24);
            _offset += 4;
        }

        public void WriteI32 (int v) {
            CheckCapacity (4);
            _data[_offset] = (byte) v;
            _data[_offset + 1] = (byte) (v >> 8);
            _data[_offset + 2] = (byte) (v >> 16);
            _data[_offset + 3] = (byte) (v >> 24);
            _offset += 4;
        }

        public float ReadF32 () {
            F32Converter conv = default;
            conv.Uint = ReadU32 ();
            return conv.Float;
        }

        public void WriteF32 (float v) {
            CheckCapacity (4);
            F32Converter conv = default;
            conv.Float = v;
            WriteU32 (conv.Uint);
        }

        public double ReadF64 () {
            F64Converter conv = default;
            conv.Ulong1 = _data[_offset];
            conv.Ulong2 = _data[_offset + 1];
            conv.Ulong3 = _data[_offset + 2];
            conv.Ulong4 = _data[_offset + 3];
            conv.Ulong5 = _data[_offset + 4];
            conv.Ulong6 = _data[_offset + 5];
            conv.Ulong7 = _data[_offset + 6];
            conv.Ulong8 = _data[_offset + 7];
            _offset += 8;
            return conv.Double;
        }

        public void WriteF64 (double v) {
            CheckCapacity (8);
            F64Converter conv = default;
            conv.Double = v;
            _data[_offset] = conv.Ulong1;
            _data[_offset + 1] = conv.Ulong2;
            _data[_offset + 2] = conv.Ulong3;
            _data[_offset + 3] = conv.Ulong4;
            _data[_offset + 4] = conv.Ulong5;
            _data[_offset + 5] = conv.Ulong6;
            _data[_offset + 6] = conv.Ulong7;
            _data[_offset + 7] = conv.Ulong8;
            _offset += 8;
        }

        public string ReadS16 () {
            var len = ReadU16 ();
            var v = Encoding.UTF8.GetString (_data, _offset, len);
            _offset += len;
            return v;
        }

        public void WriteS16 (string v) {
            var b = Encoding.UTF8.GetBytes (v);
            var len = b.Length;
            if (len > ushort.MaxValue) {
                throw new Exception ("string too long");
            }
            CheckCapacity (2 + len);
            WriteU16 ((ushort) len);
            Array.Copy (b, 0, _data, _offset, len);
            _offset += len;
        }

        [StructLayout (LayoutKind.Explicit)]
        struct F32Converter {
            [FieldOffset (0)]
            public float Float;
            [FieldOffset (0)]
            public uint Uint;
        }

        [StructLayout (LayoutKind.Explicit)]
        struct F64Converter {
            [FieldOffset (0)]
            public double Double;
            [FieldOffset (0)]
            public byte Ulong1;
            [FieldOffset (1)]
            public byte Ulong2;
            [FieldOffset (2)]
            public byte Ulong3;
            [FieldOffset (3)]
            public byte Ulong4;
            [FieldOffset (4)]
            public byte Ulong5;
            [FieldOffset (5)]
            public byte Ulong6;
            [FieldOffset (6)]
            public byte Ulong7;
            [FieldOffset (7)]
            public byte Ulong8;
        }
    }
}