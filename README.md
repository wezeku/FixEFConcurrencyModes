FixEFConcurrencyMode
====================

When using Entity Framework, the ConcurrencyMode attribute for properties
mapped to rowversion (a.k.a. timestamp) database columns, should be set to
ConcurrencyMode.Fixed, to enable optimistic concurrency.

Unfortunately, due to a bug in the Entity Framework tools, that property is 
set to ConcurrencyMode.None when you generate an edmx file by reverse 
engineering a database.

This utility corrects the faulty ConcurrencyMode settings in your edmx files.

<a href="http://blog.wezeku.com/2014/04/28/fixefconcurrencymode/" target="_blank">See this blog post for more information.</a>
