using BenchmarkDotNet.Running;

// Allow to pass in arguments from the command line
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

