namespace SKSSL.Utilities;

/// <summary>
/// Convenient iterator.
/// </summary>
/// <param name="InitialId"></param>
/// <param name="Maximum"></param>
public class IDIterator(int InitialId = 0, int Maximum = -1)
{
    #region Operator Overloads

    public static implicit operator int(IDIterator iterator) => iterator.ID;
    public static implicit operator IDIterator(int id) => new(id);
    public static IDIterator operator ++(IDIterator iterator) => iterator.Iterate();

    #endregion

    public int ID { get; internal set; } = InitialId;
    public int Iterate()
    {
        int nextId = ID++;
        if (Maximum != -1 && ID > Maximum)
            throw new IndexOutOfRangeException("Too many voxel definitions for maximum voxel count!");
        return nextId;
    }

    public override string ToString() => ID.ToString();
}