namespace SKSSL.Utilities;

/// <summary>
/// Convenient iterator.
/// </summary>
/// <param name="InitialId"></param>
/// <param name="Maximum"></param>
public class IDIterator(int InitialId = 0, int Maximum = -1)
{
    public int _nextId = InitialId;
    public int Iterate()
    {
        int id = _nextId++;
        if (Maximum != -1 && _nextId >= Maximum)
            throw new IndexOutOfRangeException("Too many voxel definitions for maximum voxel count!");
        return id;
    }
}