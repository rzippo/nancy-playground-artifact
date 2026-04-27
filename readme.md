# Nancy-Playground: A Console Calculator for Deterministic Network Calculus — Artifact

*Raffaele Zippo* and *Giovanni Stea*

## Contents

This artifact supports the paper *Nancy-Playground: A Console Calculator for Deterministic Network Calculus* (ECRTS 2026).
It is composed of:

- the source code of `nancy-playground` (version 1.0.8), a cross-platform CLI tool for Deterministic Network Calculus (DNC) computations using the MPPG scripting syntax;
- example scripts from the paper, clearly labelled by listing number, in the [`paper-examples`](/paper-examples/) directory;
- a benchmark script (`benchmark-mppg.ps1`) to reproduce the performance measurements in Table 6 of the paper.

The full MPPG syntax reference is in [`syntax.md`](syntax.md).

## Claims

The paper presents `nancy-playground` as an open-source, locally runnable tool that enables researchers to use the same scripting syntax as RTaW's min-plus playground.
We describe here the *qualitative* and *quantitative* claims of the paper.

### Qualitative claims

1. The `interactive` mode can be used to write MPPG scripts, line by line, aided by the included `!help` documentation
2. The `run` command can be used to parse and execute entire MPPG scripts from file
3. The `convert` command can be used to convert and MPPG script to an equivalent C# program, for further development

### Quantitative claims

1. The correctness of the MPPG parsing and computation is verified via a large set of representative [tests](/Nancy-Playground/Nancy-Playground.Tests/). 
   In particular, the `RunCommandGoldenTests` verify that the `nancy-playground` produces the same results as [RTaW's min-plus playground](http://realtimeatwork.com/minplus-playground), and the `ConvertCommand*Tests` verify that the converted C# program produces the same results as the `run` command on the same script.
   The current code passes 100% of these tests.
2. We measure the test coverage, i.e. how much of the codebase is covered by the current suite of tests, as commented in Section 4.5 of the paper.
   The current test suite covers 67 % of the codebase. 
3. Interpreting scripts via `run` incurs in a measurable performance penalty over an equivalent C# program, such as the one produced via `convert`. However, such penalty is negligible in the context of prototyping and experimentation.

## Requirements

We provide two main ways of reproducing these results: using the published version of `nancy-playground`, directly on one's machine, or using the provided `Dockerfile` with all necessary tools.

### Option A: Installing `nancy-playground`

`nancy-playground` is published on NuGet as a .NET Tool, which can be installed on any system with [.NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) installed.
This method is recommended to verify the qualitative claims, as it makes easier to use `plot()`, which will open the generated plots using your default image viewer.


First, install the [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0), then install the tool using 

```
dotnet tool install --global unipi.nancy.playground.cli
```

Then, run `nancy-playground --version` to verify that the installation was successful.

```
> nancy-playground --version
This is nancy-playground, version 1.0.8 (7508e35).
```

### Option B: Using the `Dockerfile`

The provided `Dockerfile` installs all tools required to build and run `nancy-playground` from source.
This method is recommended to verify the quantitative claims, as it makes easier to run the provided scripts.

First, move to this directory using `cd`.
Then, build the image using

```
docker build . -t nancy-playground
```

Then, start a container with:

```
docker run -it -v $(pwd .):/home/dotnet/nancy-playground-artifact -w /home/dotnet/nancy-playground-artifact nancy-playground bash
```

In this environment, you will also be able to install the published version of `nancy-playground`, running the same commands listed for Option A.
However, note that `plot()` will likely not work, because (in most setups) the docker container is not able to open a graphical window.

## Reproducing the results

### Qualitative claims

To verify the qualitative claims, we suggest starting from the examples provided in the paper, which are available here in [`paper-examples`](/paper-examples/).
One can then compare the results of these script when run with `nancy-playground` and when run with [RTaW's min-plus playground](http://realtimeatwork.com/minplus-playground).

#### Launching `nancy-playground`

If the published version is installed as a .NET Tool, then one only need to launch it as `nancy-playground`

```
nancy-playground --help
```

To compile and run from source, instead, you need to use `dotnet run`

```
dotnet run --project Nancy-Playground/Nancy-Playground/Nancy-Playground.csproj --framework net10.0 -- --help
```

Note that everything after the `--` is interpreted as arguments for `nancy-playground`.

#### Testing `run`

The `run` command takes a script as argument, and executes it.

```
nancy-playground run ./paper-examples/listing-3-wcd-single-node.mppg
```

See `nancy-playground run --help` for more info.

#### Testing `interactive`

With the `interactive` command the program acts as an interactive shell.
MPPG scripts can be written one line at a time, with immediate feedback.
Use `!help` to see the documentation, which includes other interactive-only commands like `!load`, `!save` and `!exit`.

See `nancy-playground interactive --help` for more info.

#### Testing `convert`

The `convert` command takes a script as argument, and produces an equivalent C# program.

```
nancy-playground convert ./paper-examples/listing-1a-mppg-syntax.mppg
```

The output is a `.cs` file which, by default, shares location and name of the input script.
This can then be run, as any file-based C# program, using `dotnet run`

```
dotnet run ./paper-examples/listing-1a-mppg-syntax.mppg.cs
```

See `nancy-playground convert --help` for more info.

### Quantitative claims

> The scripts provided are [Powershell](https://learn.microsoft.com/en-us/powershell/scripting/install/installing-powershell) scripts.
> Being included in the `Dockerfile`, no further installation is required if you use Option B.
> The scripts start with the shebang `#!/bin/pwsh`, so there is no need to change shell to launch them. 

To verify claims 1 and 2, we provide the [`run-tests-and-coverage-report.ps1`](./run-tests-and-coverage-report.ps1) script, which will both run the tests and produce the coverage report.

```
./run-tests-and-coverage-report.ps1
```

As it runs both the tests themselves and the collection of coverage statistics, the script will take around ten minutes to run.
It should then report that all tests passed, and produce a summary of the coverage results.
For a more detailed and interactive breakdown, use a browser to open the `index.html` file produce in `./coveragereport`.
These results should be compared to Table 7 of the paper.

To verify claim 3, we provide [`benchmark-np-run-vs-convert.ps1`](./benchmark-np-run-vs-convert.ps1) which, given a script, benchmarks the runtime and peak memory utilization of 1) the `run` command, 2) the `convert` command, 3) the C# code produce by `convert`.

```
./benchmark-np-run-vs-convert.ps1 ./paper-examples/guidolin--pina-phd-listing-b.1.mppg
```

The results of the above should be compared to Table 6 of the paper, which indeed used [`guidolin--pina-phd-listing-b.1.mppg`](./paper-examples/guidolin--pina-phd-listing-b.1.mppg) as benchmark example.
The benchmark can be run with any other MPPG script, which should produce different values but confirm the same trend.

## Documentation

The `syntax.md` file in this repository documents all supported MPPG constructs.
The integrated help can be accessed at any time with `nancy-playground interactive` followed by `!help`.
The online documentation for Nancy and related libraries and tools, including `nancy-playground`, is at [nancy.unipi.it](https://nancy.unipi.it). 

## References

- R. Zippo and G. Stea. *Nancy-Playground: A Console Calculator for Deterministic Network Calculus*. ECRTS 2026.
- R. Zippo and G. Stea. *Nancy: an efficient parallel Network Calculus library*. SoftwareX, 2022. DOI: 10.1016/j.softx.2022.101178
- A. Bouillard et al. *Deterministic Network Calculus: From Theory to Practical Implementation*. Wiley, 2018.
