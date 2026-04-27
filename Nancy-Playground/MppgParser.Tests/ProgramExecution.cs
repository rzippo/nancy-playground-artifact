
namespace Unipi.Nancy.Playground.MppgParser.Tests;

public class ProgramExecution
{
    private readonly ITestOutputHelper _testOutputHelper;

    public ProgramExecution(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    public static List<string> Programs =
    [
        """
        T4 := 60
        A1 := stair(0, 60, 35)
        A2 := stair (0, 5, 2)
        A4 := stair (0, T4, 12)
        C := affine (1 ,0)
        D1 := C + (A1 - C)*zero
        D2 := C + (A1 + A2 - C)*zero - D1
        D4 := C + (A4 - C)*zero
        floor := right-ext(stair(1, 1, 1))
        A3 := ( floor comp (D2 / 2) ) * 4
        D3 := C + (A3 + A4 - C)*zero - D4
        hDev(A3 , D3)
        """,
        """
        T4 := 60
        A1 := stair(0, 60, 35)
        A2 := stair (0, 5, 2)
        A4 := stair (0, T4, 12)
        C := affine (1 ,0)
        D1 := C + (A1 - C)*zero
        D2 := C + (A1 + A2 - C)*zero - D1
        D4 := C + (A4 - C)*zero
        floor := right-ext(stair(1, 1, 1))
        A3 := ( floor comp (D2 / 2) ) * 4
        D3 := C + (A3 + A4 - C)*zero - D4
        h := hDev(A3 , D3)
        printExpression(h)
        h
        """
    ];

    public static IEnumerable<object[]> ProgramTestCases =
        Programs.ToXUnitTestCases();

    [Theory]
    [MemberData(nameof(ProgramTestCases))]
    public void ProgramExecutionToStringOutput(string programText)
    {
        var program = Program.FromText(programText);
        var output = program.ExecuteToStringOutput();
        foreach (var line in output)
        {
            _testOutputHelper.WriteLine(line);
        }
    }
}