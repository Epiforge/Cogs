namespace Gear.ActiveQuery
{
    /// <summary>
    /// Describes the indexing strategies for active queries
    /// </summary>
    public enum IndexingStrategy
    {
        /// <summary>
        /// Don't perform indexing or base indexing on the source of the query
        /// </summary>
        NoneOrInherit,

        /// <summary>
        /// Index using a hash table
        /// </summary>
        HashTable,

        /// <summary>
        /// Index using a self-balancing binary search tree
        /// </summary>
        SelfBalancingBinarySearchTree
    }
}
