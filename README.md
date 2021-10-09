## DepChecker

A command line tool for viewing assembly references and problems.
DepChecker is compatible with `.Net Core` allowing it to be used in environments other than windows.

## Usage

Run DepChecker from the command line and provide it a path of assemblies to examine.
```
    ./DepChecker some/directory/to/inspect
```

Is there are no issues with your dependencies, then you will get output similar to this:
```
[DepChecker 1.0.0.0] <- File
    [System.Runtime 5.0.0.0] <- Runtime
    [System.Collections 5.0.0.0] <- Runtime
    [System.Console 5.0.0.0] <- Runtime
    [System.IO.FileSystem 5.0.0.0] <- Runtime

```

If the dependency tree has issues, then those will be logged in the tree:
```
# If the correct version could not be found.
[System.Runtime.CompilerServices.Unsafe 4.0.4.1] <- IncorrectVersion
# If the assembly could not be found at all.
[System.Runtime.CompilerServices.Unsafe 4.0.4.1] <- NotFOund
```

If your project has dependency issues, then you will get additional output:
```
Could not locate [System.Runtime.CompilerServices.Unsafe, 4.0.4.1]:
Expected by:
        System.Memory, Version=4.0.1.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
        System.Threading.Tasks.Extensions, Version=4.2.0.1, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
        System.Text.Json, Version=5.0.0.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51
```

DepChecker will also treat items which loaded as redirects as errors and produce additional output.

In this sample we expected `4.0.0.0` of `System.Net.Http` but `5.0.0.0` is what was loaded.
```
Assembly Loaded Via Redirect [System.Net.Http, 4.0.0.0]:
Expected by:
        Example.Internal, Version=2.1.1.0, Culture=neutral, PublicKeyToken=cc7b13ffcd2ddd51 
                  Version Expected: System.Net.Http, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a
```