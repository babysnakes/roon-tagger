namespace RoonTagger.Build.Help
{

    using Spectre.Console;
    using static Spectre.Console.AnsiConsole;

    public static class ConsoleHelpers
    {
        public static void MainHelp()
        {
            var star = "[green]*[/]";

            var gr1 = AddTextGrid();
            gr1.AddRow("[green italic]Roon Tagger[/] Build System.");
            gr1.AddEmptyRow();
            string[] desc = {
                "This build system is targetted toward CI checks and publishing artifacts.",
                "It's not optimized for running during development.",
                "For publishing or releasing it requires a tag that points to",
                "the current [italic]HEAD[/], Otherwise it fails (This could be overriden, see options).",
                "This tag is used as the version in the artifact's name and as the tag in the release."
            };
            gr1.AddRow(string.Join(" ", desc));
            gr1.AddEmptyRow();
            gr1.AddRow("[italic]Main Targets[/]:");

            var gr2 = AddOptionsGrid();
            gr2.AddRow($"{star} Check:", "Run various checks. Fails if checks fail. Good for CI.");
            gr2.AddRow($"{star} Publish:", "Create compressed distributions for all supported architectures.");
            gr2.AddRow($"{star} todo:", "...");
            gr2.AddEmptyRow();

            var gr3 = AddTextGrid();
            gr3.AddRow("[italic]Global Options[/]:");

            var gr4 = AddOptionsGrid();
            gr4.AddRow($"{star} --release:", "Work on [italic]Release[/] configuration. Default is [italic]Debug[/].");
            gr4.AddRow($"{star} --clean:", "Run the [italic]Clean[/] task before relevant tasks.");
            gr4.AddRow($"{star} --override-version:", "Specify version to use. Overrides the current tag in the repository.");

            Grid[] grids = { gr1, gr2, gr3, gr4 };
            foreach (Grid g in grids)
            {
                Write(g);
            }
        }

        private static Grid AddOptionsGrid()
        {
            var grid = new Grid();
            grid.AddColumn(new GridColumn().NoWrap());
            grid.AddColumn(new GridColumn().PadLeft(2));
            return grid;
        }

        private static Grid AddTextGrid()
        {
            var grid = new Grid();
            grid.AddColumn(new GridColumn());
            return grid;
        }
    }
}