# DataFirst
Abstracts the SQL data connection layer to make it easier to write data models for pre-existing databases.

Work-in-progress at this stage. In many cases, especially when using MVC, it's advisable to operate in a
code-first environment, using stuff like Entity Framework to both speed up the database design and also
keep things tightly integrated.

However, there are plenty of real-world scenarios where this just isn't possible. Often when working on a
new project, you'll find that there are pre-existing database tables from legacy systems that cannot be
removed or superceeded. Similarly, sometimes the database setup is so complex that it pays to develop that
first in SQL and then connect up your data model afterwards.

DataFirst is a collection of functions that make hooking properties in C# classes up to databases as easy
as possible.
