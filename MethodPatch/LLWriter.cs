using System;

namespace MethodPatch
{
    public unsafe sealed class LLWriter
    {
        public IntPtr Position { get; private set; }
        public int Size { get; private set; }

        private IntPtr _startPosition;

        public LLWriter(IntPtr position, int size)
        {
            Set(position, size);
        }

        public int AvailableSize => (int)(_startPosition.ToInt64() + Size - Position.ToInt64());

        public void Set(IntPtr position, int size)
        {
            _startPosition = Position = position;
            Size = size;
        }

        public LLWriter WriteByte(byte value)
        {
            CheckAvailableSize(sizeof(byte));
            *(byte*)Position = value;
            MoveRelative(sizeof(byte));
            return this;
        }

        public LLWriter WriteInt(int value)
        {
            CheckAvailableSize(sizeof(int));
            *(int*)Position = value;
            MoveRelative(sizeof(int));
            return this;
        }

        public LLWriter WriteLong(long value)
        {
            CheckAvailableSize(sizeof(long));
            *(long*)Position = value;
            MoveRelative(sizeof(long));
            return this;
        }

        public LLWriter WriteIntPtr(IntPtr value)
        {
            CheckAvailableSize(sizeof(IntPtr));
            *(IntPtr*)Position = value;
            MoveRelative(sizeof(IntPtr));
            return this;
        }

        public LLWriter WriteInstruction(Instruction value)
        {
            return WriteByte((byte)value);
        }

        public LLWriter WriteRelativePtr(IntPtr address, int offset)
        {
            return WriteIntPtr(AssemblerHelper.GetRelativePtr(Position, address, offset));
        }

        public LLWriter WriteRelativePtr(IntPtr address)
        {
            return WriteIntPtr(AssemblerHelper.GetRelativePtr(Position, address, IntPtr.Size));
        }

        public void MoveRelative(int size)
        {
            Position += size;
        }

        private void CheckAvailableSize(int size)
        {
            if (AvailableSize < size)
                throw new Exception("Not enough space");
        }
    }
}
