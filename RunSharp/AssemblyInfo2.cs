using System.Security.Permissions;

#if !PHONE8 && !SILVERLIGHT && !NET5_0 && !NETSTANDARD
[assembly: SecurityPermission(SecurityAction.RequestMinimum, Execution = true)]
[assembly: ReflectionPermission(SecurityAction.RequestMinimum, ReflectionEmit = true)]
#endif
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames", Justification = "Not yet.")]
