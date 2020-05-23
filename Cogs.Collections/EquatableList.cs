using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace Cogs.Collections
{
    /// <summary>
    /// Represents a strongly typed list of objects that can be compared to other lists
    /// </summary>
    /// <typeparam name="T">The type of the elements in the list</typeparam>
    public struct EquatableList<T> : IReadOnlyList<T>, IEquatable<EquatableList<T>>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="EquatableList{T}"/> class that contains elements copied from the specified collection
        /// </summary>
        /// <param name="elements">The collection whose elements are copied</param>
        public EquatableList(IReadOnlyList<T> elements)
        {
            EqualityComparer = null;
            if (elements is null)
                throw new ArgumentNullException(nameof(elements));
            this.elements = elements.ToImmutableArray();
            hashCode = HashCode.Combine(typeof(EquatableList<T>), this.elements.FirstOrDefault());
            foreach (var element in this.elements.Skip(1))
                hashCode = HashCode.Combine(hashCode, element);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EquatableList{T}"/> class that contains elements copied from the specified collection and compares elements using the specified equality comparer
        /// </summary>
        /// <param name="elements">The collection whose elements are copied</param>
        /// <param name="equalityComparer">The equality comparer to use to determine whether elements are equal</param>
        public EquatableList(IReadOnlyList<T> elements, IEqualityComparer<T> equalityComparer)
        {
            EqualityComparer = equalityComparer;
            if (elements is null)
                throw new ArgumentNullException(nameof(elements));
            this.elements = elements.ToImmutableArray();
            hashCode = HashCode.Combine(typeof(EquatableList<T>), EqualityComparer.GetHashCode(this.elements.FirstOrDefault()));
            foreach (var element in this.elements.Skip(1))
                hashCode = HashCode.Combine(hashCode, EqualityComparer.GetHashCode(element));
        }

        readonly IReadOnlyList<T> elements;
        readonly int hashCode;

        /// <summary>
        /// Gets the element at the specified index
        /// </summary>
        /// <param name="index">The zero-based index of the element to get</param>
        /// <returns>The element at the specified index</returns>
        public T this[int index] => elements[index];

        /// <summary>
        /// Gets the number of elements contained in the <see cref="EquatableList{T}"/>
        /// </summary>
        public int Count => Elements.Count;

        IReadOnlyList<T> Elements => elements ?? Enumerable.Empty<T>().ToImmutableArray();

        /// <summary>
        /// Gets the equality comparer used to determine whether elements are equal if one was specified when the <see cref="EquatableList{T}"/> was instantiated
        /// </summary>
        public IEqualityComparer<T>? EqualityComparer { get; }

        /// <summary>
        /// Determines whether the specified object is equal to the current object
        /// </summary>
        /// <param name="obj">The object to compare with the current object</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c></returns>
        public override bool Equals(object obj) => obj is EquatableList<T> other ? Equals(other) : false;

        /// <summary>
        /// Determines whether the specified <see cref="EquatableList{T}"/> is equal to the current <see cref="EquatableList{T}"/> using the current <see cref="IEqualityComparer{T}"/> (see <see cref="EqualityComparer"/>)
        /// </summary>
        /// <param name="other">The <see cref="EquatableList{T}"/> to compare with the current <see cref="EquatableList{T}"/></param>
        /// <returns><c>true</c> if the specified <see cref="EquatableList{T}"/> is equal to the current <see cref="EquatableList{T}"/>; otherwise, <c>false</c></returns>
        public bool Equals(EquatableList<T> other) => (EqualityComparer is { } && EqualityComparer.Equals(other.EqualityComparer) && Elements.SequenceEqual(other.Elements, EqualityComparer)) || (other.EqualityComparer is null && Elements.Equals(other.Elements));

        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="EquatableList{T}"/>
        /// </summary>
        public IEnumerator<T> GetEnumerator() => Elements.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => Elements.GetEnumerator();

        /// <summary>
        /// Gets a hash code for the <see cref="EquatableList{T}"/>
        /// </summary>
        public override int GetHashCode() => hashCode;

        /// <summary>
        /// Determines whether two specified instances of <see cref="EquatableList{T}"/> are equal
        /// </summary>
        /// <param name="a">The first object to compare</param>
        /// <param name="b">The second object to compare</param>
        /// <returns><c>true</c> if <paramref name="a"/> and <paramref name="b"/> represent the list; otherwise, <c>false</c></returns>
        public static bool operator ==(EquatableList<T> a, EquatableList<T> b) => a.Equals(b);

        /// <summary>
        /// Determines whether two specified instances of <see cref="EquatableList{T}"/> are not equal
        /// </summary>
        /// <param name="a">The first object to compare</param>
        /// <param name="b">The second object to compare</param>
        /// <returns><c>true</c> if <paramref name="a"/> and <paramref name="b"/> do not represent the list; otherwise, <c>false</c></returns>
        public static bool operator !=(EquatableList<T> a, EquatableList<T> b) => !a.Equals(b);
    }
}
