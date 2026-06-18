namespace UI.Interfaces;

public interface IFilePicker
{
    /// <summary>
    /// Returns the file path
    /// </summary>
    /// <returns></returns>
    Task<string> PickFile();

    Task<Stream> GetReadableStream();
}