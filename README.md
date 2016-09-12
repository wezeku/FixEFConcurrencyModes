FixEFConcurrencyModes
=====================

When using Entity Framework, the ConcurrencyMode attribute for properties
mapped to rowversion (a.k.a. timestamp) database columns, should be set to
ConcurrencyMode.Fixed, to enable optimistic concurrency.

Unfortunately, due to a bug in the Entity Framework tools, that property is 
set to ConcurrencyMode.None when you generate an edmx file by reverse 
engineering a database.

This utility corrects the faulty ConcurrencyMode settings in your edmx files.

Usage
-----

Basic usage is simple. Use the -i option to specify input file, like so
```
FixEFConcurrencyModes -i MyEdmxFile.edmx
```
This will change MyEdmxFile.edmx so that all columns of type rowversion or 
timestamp will have ConcurrencyMode = Fixed. If this default behaviour doesn’t 
suit you, there are a few options.

```
-i <filename>               Edmx input file. Required.
-o <filename>               Output file. Default: overwrite the input file.
-n <pattern1 pattern2 ...>  Regex patterns for column names used for concurrency control.
-t <type1 type2 ...>        Types for concurrency control. Default: rowversion timestamp
-p                          Preview. Changes are shown but nothing is written to file.
-q                          Quiet mode.
--help                      Display the help screen.
```

A few examples
--------------

The following will set ConcurrencyMode = Fixed on all columns of type uniqueidentifier or timestamp:
```
FixEFConcurrencyModes -i MyEdmxFile.edmx -t uniqueidentifier timestamp
```


This will set ConcurrencyMode = Fixed on all columns with names that include the substring “VersionNo” or end with “Revision”:
```
FixEFConcurrencyModes -i myfile.edmx -n VersionNo Revision$
```


If you want to preview what FixEFConcurrencyModes will change, but don’t want to save the changes, use the -p option:
```
FixEFConcurrencyModes -i myfile.edmx -p
```

<a href="http://blog.wezeku.com/2014/04/28/fixefconcurrencymode/" target="_blank">Associated blog post.</a>
