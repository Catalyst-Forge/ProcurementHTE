namespace ProcurementHTE.Core.Interfaces {
    public interface ITemplateProvider {
        Task<string> GetTemplateAsync(string templateName, CancellationToken ct = default);
    }
}
