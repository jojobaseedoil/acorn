using Acorn.Dataverse.Cli.Planning;

namespace Acorn.Dataverse.Cli.Commands;

internal static class PlanPrinter
{
    public static void Print(DeploymentPlan plan)
    {
        var package = plan.Package;
        var upload = package.Kind == ChangeKind.Update ? " (content upload)" : string.Empty;
        Console.WriteLine($"  {Symbol(package.Kind)} package {package.UniqueName} {package.Version}{upload}");
        Console.WriteLine();

        foreach (var step in plan.Steps)
        {
            Console.WriteLine($"  {Symbol(step.Kind)} {step.DisplayName}  [{Describe(step)}]{Suffix(step.Kind)}");
            foreach (var difference in step.Differences)
            {
                Console.WriteLine($"        {difference}");
            }

            foreach (var image in step.Images)
            {
                Console.WriteLine($"      {Symbol(image.Kind)} image '{image.Alias}'{Suffix(image.Kind)}");
                foreach (var difference in image.Differences)
                {
                    Console.WriteLine($"            {difference}");
                }
            }
        }

        var kinds = plan.Steps.Select(s => s.Kind)
            .Concat(plan.Steps.SelectMany(s => s.Images).Select(i => i.Kind))
            .ToList();

        Console.WriteLine();
        Console.WriteLine(
            $"Plan: {Count(kinds, ChangeKind.Create)} to create, {Count(kinds, ChangeKind.Update)} to update, " +
            $"{Count(kinds, ChangeKind.Delete)} to delete, {Count(kinds, ChangeKind.Orphan)} orphan(s) kept.");

        foreach (var error in plan.Errors)
        {
            Console.Error.WriteLine($"error: {error}");
        }
    }

    private static string Describe(StepChange step)
    {
        var stage = step.Desired?.Stage ?? step.Current!.Stage;
        var mode = step.Desired?.Mode ?? step.Current!.Mode;
        return $"{stage}, {mode}";
    }

    private static string Symbol(ChangeKind kind) => kind switch
    {
        ChangeKind.Create => "+",
        ChangeKind.Update => "~",
        ChangeKind.Delete => "-",
        ChangeKind.Orphan => "!",
        _ => "=",
    };

    private static string Suffix(ChangeKind kind)
        => kind == ChangeKind.Orphan ? "  (not declared: kept, use --prune to delete)" : string.Empty;

    private static int Count(List<ChangeKind> kinds, ChangeKind kind) => kinds.Count(k => k == kind);
}