namespace UI.Interfaces;

public interface IExportService
{
    Task ToCsv(IQueryable query, string fileName = null);
    Task ToExcel(IQueryable query, string fileName = null);
}