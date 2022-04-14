using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetLocalAdmins
{
    class Program
    {
         // ToDo: Note if an account is disabled
         // ToDo: max screen
         // ToDo: Listed the different domains the users are from (can you have a user in a group that's not a trusted domain? is trust only validated when it's inserted?)
        
        static string formatString = "|{0,25} |{1,18} |{2,18} |{3,18} |{4,48} | {5}  ";
        static string logPath = @"Output.log";
        static void Log(string message)
        {
            Console.WriteLine(message);
            if (!File.Exists(logPath))
            {
                // Create a file to write to.
                using (StreamWriter sw = File.CreateText(logPath))
                {
                    sw.WriteLine(message);
                }
            }

            // This text is always added, making the file longer over time
            // if it is not deleted.
            using (StreamWriter sw = File.AppendText(logPath))
            {
                sw.WriteLine(message);
            }

        }
        internal static void GetMembers(string sid, string highlightSid = "", string remoteMachine ="")
        {
            // https://docs.microsoft.com/en-us/windows/win32/ad/naming-properties
            // UserPrincipalName - User's Logon name. Convention is sean.pierce@examble.com (UPN format)
            // SamAccountName - the backwards compatable logon name (NT 4.0, under 20 chars). Convention is example\sean.pierce or 'down-level logon name'.
            //                  The the domain in this format is the NetBIOS name - 15 characters max, cannot contain dots, underscores etc.
            // DistinguishedName - AD LDAP Path

            // When doing auth with SamAccountName vs. UserPrincipalName:  If there are replication issues or you can't reach a global catalog,
            // the backslash format might work in cases where the UPN format will fail. There may also be (abnormal) conditions under which the
            // reverse applies - perhaps if no domain controllers can be reached for the target domain, for example.
            // From: https://serverfault.com/questions/371150/any-difference-between-domain-username-and-usernamedomain-local

            string message = String.Format(formatString, "UserPrincipalName", "SamAccountName", "DisplayName",  "Name", "SID", "DistinguishedName" );
            int tableHeaderWidth = message.Length;
            Log(new string( '_',tableHeaderWidth));
            Log(message);

            try
            {
                PrincipalContext ctx = null;
                if (remoteMachine == "")
                {
                    ctx = new PrincipalContext(ContextType.Machine);
                } else
                {
                    // Not tested
                    ctx = new PrincipalContext(ContextType.Domain, remoteMachine);  
                    if(ctx == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Log("Failed to access: " + remoteMachine);
                        Console.ForegroundColor = ConsoleColor.Gray;
                        return;
                    }
                }
                GroupPrincipal adminGroup = GroupPrincipal.FindByIdentity(ctx, IdentityType.Sid, sid);
                var adminMembers = adminGroup.GetMembers(true);
                
                // print table
                foreach (Principal principal in adminMembers)
                {
                    message = String.Format(formatString, principal.UserPrincipalName, principal.SamAccountName,  principal.DisplayName,  principal.Name, principal.Sid, principal.DistinguishedName);
                    if (principal.Sid.ToString() == highlightSid)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        message = message.Replace("|", "*");
                    }
                    
                        
                    Log(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }


                adminMembers.Dispose();
                ctx.Dispose();

            }
            catch (NullReferenceException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log("Failed to find group: " + sid);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Log("Failed to get group members: " + ex.Message);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
        }


        static void Main(string[] args)
        {
            // Todo: high light a user via sid
            // Todo: see if there's an easy way to get this information remotely
            var remoteHostList = new List<string>();
            foreach(string arg in args)
            {
                if(arg == "-h" || arg == "--help")
                {
                    printBanner();
                    return;
                } else
                {
                    remoteHostList.Add(arg);
                }
            }


            // This users information:
            // "UserPrincipalName", "SamAccountName", "DisplayName",  "Name", "SID", "DistinguishedName" );
            string upn = System.DirectoryServices.AccountManagement.UserPrincipal.Current.UserPrincipalName;
            string sam = System.DirectoryServices.AccountManagement.UserPrincipal.Current.SamAccountName;
            string displayName = System.DirectoryServices.AccountManagement.UserPrincipal.Current.DisplayName;
            string name = System.DirectoryServices.AccountManagement.UserPrincipal.Current.Name;
            string sid = System.DirectoryServices.AccountManagement.UserPrincipal.Current.Sid.ToString();
            string distinguished = System.DirectoryServices.AccountManagement.UserPrincipal.Current.DistinguishedName;

            // Extra
            string givenName = System.DirectoryServices.AccountManagement.UserPrincipal.Current.GivenName;
            string email = System.DirectoryServices.AccountManagement.UserPrincipal.Current.EmailAddress;
            string employeeId = System.DirectoryServices.AccountManagement.UserPrincipal.Current.EmployeeId;

            // Interesting - look at later
            //string something = System.DirectoryServices.AccountManagement.UserPrincipal.Current.Certificates;
            //string something = System.DirectoryServices.AccountManagement.UserPrincipal.Current.Enabled;
            //string something = System.DirectoryServices.AccountManagement.UserPrincipal.Current.PasswordNeverExpires;
            //string something = System.DirectoryServices.AccountManagement.UserPrincipal.Current.PasswordNotRequired;
            //string something = System.DirectoryServices.AccountManagement.UserPrincipal.Current.PermittedWorkstations;
            //string something = System.DirectoryServices.AccountManagement.UserPrincipal.Current.SmartcardLogonRequired;

            // Print Current user info
            Log("Current User: " + upn);
            if (email != null && email.Trim() != "")
                Log("\tEmail: " + email);
            if(employeeId != null && employeeId.Trim() != "")
                Log("\tEmployee ID: " + employeeId);
            if(givenName != null && givenName.Trim() != "")
                Log("\tGiven Name: " + givenName);

            string message = String.Format(formatString, "UserPrincipalName", "SamAccountName", "DisplayName", "Name", "SID", "DistinguishedName");
            int tableHeaderWidth = message.Length;
            Log(new string('=', tableHeaderWidth));
            Log(message);


            message = String.Format(formatString,upn,sam,displayName,name,sid,distinguished);
            Log(message);
            Log("");

            if (remoteHostList.Count == 0)
                remoteHostList.Add("");
            foreach (string host in remoteHostList)
            {
                if (host.Trim() != "")
                    Log("Querying against remote host on: " + host);

                // This resursively prints the group membership of:
                // Admins
                // Look into:
                // What's S-1-5-114?
                if (host.Trim() == "")
                    Log("Administrators");
                else
                    Log("Administrators on :" + host);
                GetMembers("S-1-5-32-544", sid,host);
                Log("");

                // RDP
                // Look into:
                // S-1-5-4	Interactive
                // S-1-5-13	Terminal Server User
                // S-1-5-14	Remote Interactive Logon
                // S-1-5-32-555	Builtin\Remote Desktop Users
                // S-1-5-32-577	Builtin\RDS Management Servers

                if (host.Trim() == "")
                    Log("Remote Desktop Users");
                else
                    Log("Remote Desktop Users on: " + host);
                
                GetMembers("S-1-5-32-555", sid, host);
                Log("");



                // Remote Management Users group
                // Look into:
                // S-1-5-32-580	Builtin\Remote Management Users
                if (host.Trim() == "")
                    Log("WinRM (Not including those in the Administrators group)");
                else
                    Log("Remote Management Users on: " + host);
                GetMembers("S-1-5-32-580", sid, host);
                Log("");


                // Others that look interesting
                // S-1-5-64-10	NTLM Authentication	A SID that is used when the NTLM authentication package authenticated the client
                // S-1-5-64-21	Digest Authentication	A SID that is used when the Digest authentication package authenticated the client.
                // S-1-5-32-558	Builtin\Performance Monitor Users	An alias. Members of this group have remote access to monitor this computer.
                // S-1-5-32-559	Builtin\Performance Log Users	An alias. Members of this group have remote access to schedule logging of performance counters on this computer.
                // S-1-5-32-575	Builtin\RDS Remote Access Servers	A built-in local group. Servers in this group enable users of RemoteApp programs and personal virtual desktops access to these resources. In Internet-facing deployments, these servers are typically deployed in an edge network. This group needs to be populated on servers running RD Connection Broker. RD Gateway servers and RD Web Access servers used in the deployment need to be in this group.

            }
            Console.WriteLine("Done. Press Enter to exit");
            Console.ReadLine();

        }

        private static void printBanner()
        {
            Console.WriteLine("Only supported arguments are -h/--help and (remote) hostnames as parameters (which not working right now)");
        }
    }
}
