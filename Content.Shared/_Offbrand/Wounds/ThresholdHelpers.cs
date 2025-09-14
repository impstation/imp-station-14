/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/.
 */

using System.Linq;

namespace Content.Shared._Offbrand.Wounds;

public static class ThresholdHelpers
{
    public static TValue? HighestMatch<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey value) where TKey : IComparable<TKey> where TValue : struct
    {
        foreach (var (threshold, data) in dictionary.Reverse())
        {
            if (value.CompareTo(threshold) < 0)
                continue;

            return data;
        }

        return null;
    }

    public static TValue? LowestMatch<TKey, TValue>(this SortedDictionary<TKey, TValue> dictionary, TKey value) where TKey : IComparable<TKey> where TValue : struct
    {
        foreach (var (threshold, data) in dictionary)
        {
            if (value.CompareTo(threshold) > 0)
                continue;

            return data;
        }

        return null;
    }
}
