namespace canmarket.src.helpers.Interfaces
{
    public interface IWriteSoldLog
    {
        public void AddSoldByLog(string playerName, string goodItemName, int amount);
    }
}
