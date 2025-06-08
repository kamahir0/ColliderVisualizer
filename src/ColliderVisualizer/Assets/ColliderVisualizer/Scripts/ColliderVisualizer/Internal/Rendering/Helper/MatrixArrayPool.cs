using System;
using System.Collections.Generic;
using UnityEngine;

namespace ColliderVisualizer
{
    /// <summary>
    /// Matrix4x4配列の再利用プール
    /// </summary>
    public class MatrixArrayPool
    {
        private readonly Dictionary<int, Stack<Matrix4x4[]>> _poolDictionary = new();
        private static readonly int[] _sizeCategories = { 64, 128, 256, 512, 1024, 2048, 4096 };
        private readonly object _lock = new();
        private const int MaxPoolSizePerCategory = 4;

        /// <summary>
        /// 要求サイズに応じて、適切な長さの配列を取得する
        /// </summary>
        public Matrix4x4[] Rent(int requiredSize)
        {
            if (requiredSize <= 0) return Array.Empty<Matrix4x4>();

            var size = GetSizeCategory(requiredSize);
            
            lock (_lock)
            {
                if (_poolDictionary.TryGetValue(size, out var stack) && stack.Count > 0)
                {
                    return stack.Pop();
                }
            }
            
            return new Matrix4x4[size];
        }

        /// <summary>
        /// 取得した配列をプールに返す
        /// </summary>
        public void Return(Matrix4x4[] array)
        {
            if (array == null || array.Length == 0) return;
            
            var categorySize = GetSizeCategory(array.Length);
            
            // 配列のサイズがカテゴリサイズと一致しない場合は破棄
            if (array.Length != categorySize) return;
            
            lock (_lock)
            {
                if (!_poolDictionary.TryGetValue(categorySize, out var stack))
                {
                    stack = new Stack<Matrix4x4[]>();
                    _poolDictionary[categorySize] = stack;
                }
                
                if (stack.Count < MaxPoolSizePerCategory)
                {
                    // 配列をクリア（セキュリティ上の理由）
                    Array.Clear(array, 0, array.Length);
                    stack.Push(array);
                }
            }
        }

        // 要求サイズに応じた適切な配列長を決定する
        private static int GetSizeCategory(int requiredSize)
        {
            foreach (var category in _sizeCategories)
            {
                if (requiredSize <= category)
                    return category;
            }
            
            // 最大カテゴリより大きい場合は、次の2の累乗に切り上げ
            var powerOfTwo = 1;
            while (powerOfTwo < requiredSize)
            {
                powerOfTwo <<= 1;
            }
            return powerOfTwo;
        }
    }
}