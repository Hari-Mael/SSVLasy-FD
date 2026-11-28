using System;
using System.Collections.Generic;

namespace Sahavola
{
    public static class Audit
    {
        public static List<AuditLog> FiltrerParPseudo(List<AuditLog> logs, string pseudo) =>
            logs.FindAll(l => l.Pseudo.Equals(pseudo, StringComparison.OrdinalIgnoreCase));

        public static List<AuditLog> FiltrerParAction(List<AuditLog> logs, string action) =>
            logs.FindAll(l => l.Action == action);
    }
}
