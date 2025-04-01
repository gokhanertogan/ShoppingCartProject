namespace ShoppingCart.Domain.Common;

public interface IRepository<T, TId> where T : IEntity<TId>
{

}