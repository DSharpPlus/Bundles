// This Source Code form is subject to the terms of the Mozilla Public
// License, v. 2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at https://mozilla.org/MPL/2.0/.

namespace Bundles;

/// <summary>
/// Specifies means to compare equality of collections.
/// </summary>
public enum CollectionEqualityComparison
{
    /// <summary>
    /// Equals if the metadata and data references equal.
    /// </summary>
    Reference,

    /// <summary>
    /// Equals if the metadata and data values equal.
    /// </summary>
    Value,

    /// <summary>
    /// Equals if the right-hand collection is a slice of the left-hand collection.
    /// </summary>
    Slice
}
