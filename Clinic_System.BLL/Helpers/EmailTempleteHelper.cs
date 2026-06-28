using Microsoft.EntityFrameworkCore.Metadata.Internal;
using MimeKit;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices.JavaScript;
using System.Text;
using static Org.BouncyCastle.Math.EC.ECCurve;

namespace Clinic_System;

public static class EmailTempleteHelper
{
    public static string template = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Email Template</title>
</head>

<body style=""margin:0; padding:0; background-color:#f4f4f4; font-family:Arial, Helvetica, sans-serif;"">

    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#f4f4f4; padding:30px 0;"">
        <tr>
            <td align=""center"">

                <table width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0""
                       style=""background:#ffffff; border-radius:12px; overflow:hidden;"">

                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""background:#2563eb; color:white; padding:30px;"">
                            <h1 style=""margin:0;"">Clinic Flow</h1>
                            <p style=""margin-top:10px;"">Welcome to our platform</p>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding:40px; color:#333333;"">

                            <h2 style=""margin-top:0;"">Hello 👋</h2>

                            <p style=""font-size:16px; line-height:1.7;"">
                                بعمل test يا شباب لخدمة ال email
                            </p>

                            <p style=""font-size:14px; color:#666666;"">
                                If you did not create this account, you can safely ignore this email.
                            </p>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding:20px; background:#f9fafb; color:#888888; font-size:13px;"">
                            © 2026 Clinic Flow. All rights reserved.
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>
";
    public static string template2 = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Email Template</title>
</head>

<body style=""margin:0; padding:0; background-color:#f4f4f4; font-family:Arial, Helvetica, sans-serif;"">

    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#f4f4f4; padding:30px 0;"">
        <tr>
            <td align=""center"">

                <table width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0""
                       style=""background:#ffffff; border-radius:12px; overflow:hidden;"">

                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""background:#2563eb; color:white; padding:30px;"">
                            <h1 style=""margin:0;"">عمك احمد حسني</h1>
                            <p style=""margin-top:10px;"">Welcome to our platform</p>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding:40px; color:#333333;"">

                            <h2 style=""margin-top:0;"">Hello Ahmed 👋</h2>

                            <p style=""font-size:16px; line-height:1.7;"">
                                يابهنس يا خول
                            </p>

                            <p style=""font-size:14px; color:#666666;"">
                                If you did not create this account, you can safely ignore this email.
                            </p>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding:20px; background:#f9fafb; color:#888888; font-size:13px;"">
                            © 2026 العم احمد حسني. All rights reserved.
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>
";
    public static string template3 = @"
<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>Email Template</title>
</head>

<body style=""margin:0; padding:0; background-color:#f4f4f4; font-family:Arial, Helvetica, sans-serif;"">

    <table width=""100%"" cellpadding=""0"" cellspacing=""0"" border=""0"" style=""background-color:#f4f4f4; padding:30px 0;"">
        <tr>
            <td align=""center"">

                <table width=""600"" cellpadding=""0"" cellspacing=""0"" border=""0""
                       style=""background:#ffffff; border-radius:12px; overflow:hidden;"">

                    <!-- Header -->
                    <tr>
                        <td align=""center"" style=""background:#2563eb; color:white; padding:30px;"">
                            <h1 style=""margin:0;"">عمك احمد حسني</h1>
                            <p style=""margin-top:10px;"">Welcome to our platform</p>
                        </td>
                    </tr>

                    <!-- Body -->
                    <tr>
                        <td style=""padding:40px; color:#333333;"">

                            <h2 style=""margin-top:0;"">Hello Ahmed 👋</h2>

                            <p style=""font-size:16px; line-height:1.7;"">
                                يابهنس يا خول
                            </p>

                            <p style=""font-size:14px; color:#666666;"">
                                If you did not create this account, you can safely ignore this email.
                            </p>

                        </td>
                    </tr>

                    <!-- Footer -->
                    <tr>
                        <td align=""center"" style=""padding:20px; background:#f9fafb; color:#888888; font-size:13px;"">
                            © 2026 العم احمد حسني. All rights reserved.
                        </td>
                    </tr>

                </table>

            </td>
        </tr>
    </table>

</body>
</html>
";

}
