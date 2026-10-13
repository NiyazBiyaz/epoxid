using VerifyCS = Epoxid.SourceGeneration.Tests.CSharpSourceGeneratorVerifier<Epoxid.SourceGeneration.Generators.SlotGenerator>;

namespace Epoxid.SourceGeneration.Tests;

public class TestSlotGenerator
{
    [Theory]
    [InlineData("EmptyType")]
    [InlineData("OneSlot")]
    [InlineData("ValueTypes")]
    [InlineData("ReferenceTypes")]
    public async Task TestGenerator(string resourceName)
    {
        GeneratorTestHelper.GetResources(Path.Combine("Slots", resourceName), out var common, out var sources, out var generated);

        await VerifyCS.VerifyAsync(common, sources, generated);
    }
}
