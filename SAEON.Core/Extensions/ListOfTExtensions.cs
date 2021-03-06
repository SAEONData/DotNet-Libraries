﻿using System.Collections.Generic;

namespace SAEON.Core
{
    public static class ListOfTExtensions
    {
        public static void AddIfNotExists<T>(this List<T> list, T item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }
    }
}
