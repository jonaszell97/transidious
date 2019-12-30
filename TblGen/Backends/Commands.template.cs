
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Transidious
{
    public class DeveloperConsoleInternals
    {
        DeveloperConsole console;

        public DeveloperConsoleInternals(DeveloperConsole console)
        {
            this.console = console;
        }

        public void ParseCommand(string rawCmd)
        {
            var characters = rawCmd.ToCharArray();

            string cmd = null;
            var paramValues = new List<Tuple<string, string>>();

            var currentStr = new StringBuilder();
            for (var i = 0; i < characters.Length; ++i)
            {
                string parameterName = null;

                var done = false;
                while (!done && i < characters.Length)
                {
                    var c = characters[i];
                    switch (c)
                    {
                        case ' ':
                            done = true;
                            break;
                        case '"':
                            ++i;

                            while (i < characters.Length && characters[i] != '"')
                            {
                                currentStr.Append(characters[i]);
                                ++i;
                            }

                            break;
                        case '=':
                            if (cmd == null || currentStr.Length == 0)
                            {
                                DeveloperConsole.Log("unexpected character '='");
                                return;
                            }

                            parameterName = currentStr.ToString();
                            currentStr.Clear();
                            ++i;

                            break;
                        default:
                            currentStr.Append(c);
                            ++i;

                            break;
                    }
                }

                if (cmd == null)
                {
                    cmd = currentStr.ToString();
                    currentStr.Clear();

                    DeveloperConsole.currentCommand = cmd;

                    continue;
                }

                paramValues.Add(Tuple.Create(parameterName, currentStr.ToString()));
                currentStr.Clear();
            }

            Debug.Assert(cmd != null, "no command entered");

            switch (cmd)
            {
        <% for_each_record | "Command" as CMD %>
            case <%% str | $(CMD).cmd %%> :
            {
                var assignedParams = new Dictionary<string, string>();
                foreach (var paramValue in paramValues)
                {
                    if (paramValue.Item1 != null)
                    {
                        var isValid = false;
                        while (true)
                        {
                        <% for_each | $(CMD).params as PARM %>
                            if (paramValue.Item1 == <%% str | $(PARM).name %%>)
                            {
                                isValid = true;
                                break;
                            }
                        <% end %>

                            break;
                        }

                        if (!isValid)
                        {
                            DeveloperConsole.Log($"ignoring unknown parameter '{paramValue.Item1}'");
                        }
                        else
                        {
                            assignedParams.Add(paramValue.Item1, paramValue.Item2);
                        }
                    }
                    else
                    {
                        // Assign parameter based on position.
                        var foundUnassigned = false;

                    <% for_each | $(CMD).params as PARM %>
                        if (!assignedParams.ContainsKey(<%% str | $(PARM).name %%>))
                        {
                            foundUnassigned = true;
                            assignedParams.Add(<%% str | $(PARM).name %%>, paramValue.Item2);
                        }
                    <% end %>

                        if (!foundUnassigned)
                        {
                            DeveloperConsole.Log($"ignoring parameter '{paramValue.Item2}'");
                        }
                    }
                }

            <% for_each | $(CMD).params as PARM, i %>
                if (!assignedParams.ContainsKey(<%% str | $(PARM).name %%>)) {
                <% if | !eq($(PARM).defaultValue, "") %>
                    DeveloperConsole.Log("missing parameter '" + <%% str | $(PARM).name %%> + "'");
                    return;
                <% end %>

                    assignedParams.Add(<%% str | $(PARM).name %%>, <%% str | $(PARM).defaultValue %%>);
                }

                <%% $(PARM).type %%> <%% $(PARM).name %%>;
                <% if | !eq($(PARM).type, "int") %>
                    if (!int.TryParse(assignedParams[<%% str | $(PARM).name %%>], out <%% $(PARM).name %%>))
                    {
                        DeveloperConsole.Log($"expects integer for argument #" + <%% str | $(i) %%>);
                        return;
                    }
                <% end %>
                <% if | !eq($(PARM).type, "double") %>
                    if (!double.TryParse(assignedParams[<%% str | $(PARM).name %%>], out <%% $(PARM).name %%>))
                    {
                        DeveloperConsole.Log($"expects floating point for argument #" + <%% str | $(i) %%>);
                        return;
                    }
                <% end %>
                <% if | !eq($(PARM).type, "decimal") %>
                    if (!decimal.TryParse(assignedParams[<%% str | $(PARM).name %%>], out <%% $(PARM).name %%>))
                    {
                        DeveloperConsole.Log($"expects decimal for argument #" + <%% str | $(i) %%>);
                        return;
                    }
                <% end %>
                <% if | !eq($(PARM).type, "string") %>
                    <%% $(PARM).name %%> = assignedParams[<%% str | $(PARM).name %%>];
                <% end %>
            <% end %>

                console.Handle<%% record_name | $(CMD) %%>Command(
                    <% for_each_join | $(CMD).params as PARM, ", " %> <%% $(PARM).name %%> <% end %>
                );

                break;
            }
        <% end %>

            default:
                DeveloperConsole.Log("unknown command");
                break;
            }
        }

        public string GetCommandList()
        {
            var result = "";
        <% for_each_record | "Command" as CMD, i %>
            <% if | !ne($(i), 0) %>
            result += " ";
            <% end %>

            result += <%% str | $(CMD).cmd %%>;
        <% end %>

            return result;
        }

        public string GetHelp(string cmd)
        {
        <% for_each_record | "Command" as CMD %>
            if (cmd == <%% str | $(CMD).cmd %%>)
            {
                return <%% str | $(CMD).help %%>;
            }
        <% end %>

            return $"unknown command {cmd}";
        }
    }
}