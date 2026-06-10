namespace ProcurementHTE.Infrastructure.Data;

public static partial class ProcurementSeeder
{
    private static List<ProcurementSeedData> BuildSampleProcurements()
    {
        var samples = new List<ProcurementSeedData>();

        samples.AddRange(BuildAngkutanSamples());
        samples.AddRange(BuildStandBySamples());
        samples.AddRange(BuildMovingSamples());

        return samples;
    }
}
