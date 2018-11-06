namespace Rudel
{
    internal class VirtualOverflowArray<T>
    {
        private readonly int[] _indexes;
        private readonly T[] _array;

        public VirtualOverflowArray(int size)
        {
            _array = new T[size];
            _indexes = new int[size];
        }

        public T this[int index]
        {
            get
            {
                int arrayIndex = NumberUtils.WrapMod(index, _array.Length);

                if (_indexes[arrayIndex] == index)
                    return _array[arrayIndex];
                else
                    return default(T);
            }
            set
            {
                int arrayIndex = NumberUtils.WrapMod(index, _array.Length);
                _indexes[arrayIndex] = index;
                _array[arrayIndex] = value;
            }
        }
    }
}
