// Program automatically converted from MPPG syntax to a Nancy program
// Original source was in /home/dotnet/nancy-playground-artifact/paper-examples/listing-1a-mppg-syntax.mppg

// This is a file-based app: to run it, use the command `dotnet run file.cs`
// To extend it, it is recommended to convert it to a C# project with the command `dotnet project convert file.cs`
// Docs: https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps

#:package Unipi.Nancy@1.3.0
#:package Unipi.Nancy.Plots.ScottPlot@1.0.4

using System.Globalization;
using System.IO;
using Unipi.Nancy.NetworkCalculus;
using Unipi.Nancy.MinPlusAlgebra;
using Unipi.Nancy.Numerics;
using Unipi.Nancy.Plots.ScottPlot;

CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

// // Listing 1a from the paper: MPPG script from Guidolin-Pina et al. [guidolinpina:hal-04513292]

// // This script is the example used in the paper to compare MPPG syntax with C# code.

// // Run with: nancy-playground run listing-1a-mppg-syntax.mppg

// code for: T4 := 60
var T4 = new Rational(60);

// code for: A1 := stair ( 0 , 60 , 35 )
var A1 = new StairCurve(new Rational(35), new Rational(60)).DelayBy(new Rational(0));

// code for: A2 := stair ( 0 , 5 , 2 )
var A2 = new StairCurve(new Rational(2), new Rational(5)).DelayBy(new Rational(0));

// code for: A4 := stair ( 0 , T4 , 12 )
var A4 = new StairCurve(new Rational(12), T4).DelayBy(new Rational(0));

// code for: C := affine ( 1 , 0 )
var C = new Curve(new Sequence([new Point(0, new Rational(0)), new Segment(0, 1, new Rational(0), new Rational(1)) ]), 0, 1, new Rational(1));

// code for: D1 := C + ( A1 - C ) * zero
var D1 = C + Curve.Convolution(( A1 - C ), Curve.Zero());

// code for: D2 := C + ( A1 + A2 - C ) * zero - D1
var D2 = C + Curve.Convolution(( A1 + A2 - C ), Curve.Zero()) - D1;

// code for: D4 := C + ( A4 - C ) * zero
var D4 = C + Curve.Convolution(( A4 - C ), Curve.Zero());

// code for: floor := right-ext ( stair ( 1 , 1 , 1 ) )
var floor = (new StairCurve(new Rational(1), new Rational(1)).DelayBy(new Rational(1))).ToRightContinuous();

// code for: A3 := ( floor comp ( D2 / 2 ) ) * 4
var A3 = ( Curve.Composition(floor, ( D2 / new Rational(2) )) ) * new Rational(4);

// code for: D3 := C + ( A3 + A4 - C ) * zero - D4
var D3 = C + Curve.Convolution(( A3 + A4 - C ), Curve.Zero()) - D4;

// code for: hDev ( A3 , D3 )
Console.WriteLine(Curve.HorizontalDeviation(A3, D3));

// END OF PROGRAM
