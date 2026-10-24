using QpEnhance.Core;
using QpEnhance.Tests;

var t = new TestRunner();

OneEuroFilterTests.Run(t);
OpennessCalibratorTests.Run(t);
CheekProcessorTests.Run(t);
GazeMathTests.Run(t);
GazeAndPipelineTests.Run(t);
ConfigTests.Run(t);

Console.WriteLine();
Console.WriteLine($"{t.Passed} passed, {t.Failed} failed");
return t.Failed == 0 ? 0 : 1;
