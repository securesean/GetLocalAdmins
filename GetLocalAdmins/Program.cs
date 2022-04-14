using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GetLocalAdmins
{
    class Program
    {
        static string formatString = "|{0,18} |{1,18} |{2,18} |{3,18} |{4,46} | {5}  ";
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
            Console.WriteLine(new string( '_',tableHeaderWidth));
            Console.WriteLine(message);

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
                        Console.WriteLine("Failed to access: " + remoteMachine);
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
                        
                    Console.WriteLine(message);
                    Console.ForegroundColor = ConsoleColor.Gray;
                }


                adminMembers.Dispose();
                ctx.Dispose();

            }
            catch (NullReferenceException ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to get group: " + sid);
                Console.ForegroundColor = ConsoleColor.Gray;
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Failed to get group members: " + ex.Message);
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
            Console.WriteLine("Current User: " + upn);
            if (email != null && email.Trim() != "")
                Console.WriteLine("\tEmail: " + email);
            if(employeeId != null && employeeId.Trim() != "")
                Console.WriteLine("\tEmployee ID: " + employeeId);
            if(givenName != null && givenName.Trim() != "")
                Console.WriteLine("\tGiven Name: " + givenName);

            string message = String.Format(formatString, "UserPrincipalName", "SamAccountName", "DisplayName", "Name", "SID", "DistinguishedName");
            int tableHeaderWidth = message.Length;
            Console.WriteLine(new string('_', tableHeaderWidth));
            Console.WriteLine(message);

            Console.WriteLine(formatString,upn,sam,displayName,name,sid,distinguished);
            Console.WriteLine();

            foreach(string host in remoteHostList)
            {

                // This resursively prints the group membership of:
                // Admins
                // Look into:
                // What's S-1-5-114?
                Console.WriteLine("Admins");
                GetMembers("S-1-5-32-544", sid,host);
                Console.WriteLine();

                // RDP
                // Look into:
                // S-1-5-4	Interactive
                // S-1-5-13	Terminal Server User
                // S-1-5-14	Remote Interactive Logon
                // S-1-5-32-555	Builtin\Remote Desktop Users
                // S-1-5-32-577	Builtin\RDS Management Servers
                Console.WriteLine("RDP");
                GetMembers("S-1-5-32-555", sid, host);
                Console.WriteLine();



                // Remote Management Users group
                // Look into:
                // S-1-5-32-580	Builtin\Remote Management Users
                Console.WriteLine("WinRM");
                GetMembers("S-1-5-32-580", sid, host);
                Console.WriteLine();


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
            Console.WriteLine("Only supported arguments are -h/--help and hostnames as parameters");
        }
    }
}
