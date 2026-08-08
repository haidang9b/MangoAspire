namespace ChatAgent.App.Data.Enums;

/// <summary>
/// What a <see cref="Entities.VectorDocument"/> row was built from. Retrieval filters on
/// this so a menu question searches products while a policy question searches the
/// knowledge base, even though both live in one index.
/// </summary>
public enum VectorSourceType
{
    Product = 1,
    ProductCategory = 2,
    KnowledgeChunk = 3,
}
