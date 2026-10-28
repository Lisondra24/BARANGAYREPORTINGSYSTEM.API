using System.Net;

namespace BARANGAYREPORTINGSYSTEM.API.Services
{
    /// <summary>
    /// The look of the emails the API sends (inline styles, so it works in Gmail / Outlook / phones).
    /// To change a color or a sentence, edit it here - nothing else needs to change.
    /// </summary>
    public static class EmailTemplates
    {
        /// <summary>Sent right after a resident registers in the app.</summary>
        public static string RegistrationReceived(string fullName)
        {
            var name = WebUtility.HtmlEncode(fullName);
            return Layout(
                "We received your registration.",
                "<p style='margin:0 0 14px;font-size:20px;font-weight:bold;color:#1b2466;'>Hi " + name + ",</p><p style='margin:0 0 20px;'>Thank you for registering with <b>ManguikayConnect</b>. We have received your registration.</p><table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f6f7fb;border-left:4px solid #e9a935;border-radius:8px;margin:0 0 20px;'><tr><td style='padding:16px 18px;'><div style='font-size:12px;color:#6b7280;padding-bottom:8px;'>STATUS</div><span style='display:inline-block;background:#fff4dc;color:#a76b00;font-size:12px;font-weight:bold;letter-spacing:1px;padding:6px 14px;border-radius:999px;'>PENDING VERIFICATION</span><div style='padding-top:12px;'>An administrator will now check your ID and selfie. Once your account is approved, you can log in to the app.</div></td></tr></table><p style='margin:0;color:#6b7280;font-size:13px;'>- Barangay Manguikay</p>");
        }

        /// <summary>Sent when an admin marks a complaint as Resolved.</summary>
        public static string ComplaintResolved(string fullName, int complaintId)
        {
            var name = WebUtility.HtmlEncode(fullName);
            return Layout(
                "Your complaint has been resolved.",
                "<p style='margin:0 0 14px;font-size:20px;font-weight:bold;color:#1b2466;'>Hello " + name + ",</p><p style='margin:0 0 20px;'>Good news! Your complaint has been looked into and marked as resolved by the Barangay.</p><table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f6f7fb;border-left:4px solid #e9a935;border-radius:8px;margin:0 0 20px;'><tr><td style='padding:16px 18px;'><div style='font-size:12px;color:#6b7280;padding-bottom:4px;'>COMPLAINT NO.</div><div style='font-size:22px;font-weight:bold;color:#1b2466;padding-bottom:12px;'>#" + complaintId + "</div><span style='display:inline-block;background:#e6f6ec;color:#15803d;font-size:12px;font-weight:bold;letter-spacing:1px;padding:6px 14px;border-radius:999px;'>&#10003; RESOLVED</span></td></tr></table><p style='margin:0 0 20px;'>Open the <b>ManguikayConnect</b> app to see the details. Thank you for reporting and helping keep our community better.</p><p style='margin:0;color:#6b7280;font-size:13px;'>- Barangay Manguikay</p>");
        }

        /// <summary>Sent when an admin approves a registration.</summary>
        public static string AccountApproved(string fullName)
        {
            var name = WebUtility.HtmlEncode(fullName);
            return Layout(
                "Your account has been approved.",
                "<p style='margin:0 0 14px;font-size:20px;font-weight:bold;color:#1b2466;'>Hello " + name + ",</p><p style='margin:0 0 20px;'>Good news! Your <b>ManguikayConnect</b> account has been approved by the Barangay.</p><table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f6f7fb;border-left:4px solid #e9a935;border-radius:8px;margin:0 0 20px;'><tr><td style='padding:16px 18px;'><div style='font-size:12px;color:#6b7280;padding-bottom:8px;'>STATUS</div><span style='display:inline-block;background:#e6f6ec;color:#15803d;font-size:12px;font-weight:bold;letter-spacing:1px;padding:6px 14px;border-radius:999px;'>&#10003; APPROVED</span><div style='padding-top:12px;'>You can now log in to the app and file complaints.</div></td></tr></table><p style='margin:0;color:#6b7280;font-size:13px;'>- Barangay Manguikay</p>");
        }

        /// <summary>Sent when an admin rejects a registration.</summary>
        public static string AccountRejected(string fullName)
        {
            var name = WebUtility.HtmlEncode(fullName);
            return Layout(
                "Your registration was not approved.",
                "<p style='margin:0 0 14px;font-size:20px;font-weight:bold;color:#1b2466;'>Hello " + name + ",</p><p style='margin:0 0 20px;'>We checked your registration for <b>ManguikayConnect</b>, but it could not be approved.</p><table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#f6f7fb;border-left:4px solid #e9a935;border-radius:8px;margin:0 0 20px;'><tr><td style='padding:16px 18px;'><div style='font-size:12px;color:#6b7280;padding-bottom:8px;'>STATUS</div><span style='display:inline-block;background:#fde8e8;color:#b42318;font-size:12px;font-weight:bold;letter-spacing:1px;padding:6px 14px;border-radius:999px;'>NOT APPROVED</span><div style='padding-top:12px;'>Please visit the Barangay office with a valid ID so your details can be checked.</div></td></tr></table><p style='margin:0;color:#6b7280;font-size:13px;'>- Barangay Manguikay</p>");
        }

        private static string Layout(string preheader, string content) =>
            "<!DOCTYPE html><html><head><meta charset='utf-8'><meta name='viewport' content='width=device-width, initial-scale=1'></head><body style='margin:0;padding:0;background:#eef1fb;'><div style='display:none;max-height:0;overflow:hidden;opacity:0;color:transparent;'>" + WebUtility.HtmlEncode(preheader) + "</div><table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='background:#eef1fb;padding:24px 12px;'><tr><td align='center'><table role='presentation' width='100%' cellpadding='0' cellspacing='0' style='max-width:560px;background:#ffffff;border-radius:14px;overflow:hidden;font-family:Arial,Helvetica,sans-serif;'><tr><td style='background:#1b2466;padding:28px 32px;'><div style='font-size:11px;letter-spacing:2px;color:#e9a935;font-weight:bold;'>OFFICIAL BARANGAY PORTAL</div><div style='font-size:24px;color:#ffffff;font-weight:bold;padding-top:6px;'>Barangay Manguikay</div></td></tr><tr><td style='background:#e9a935;height:4px;font-size:0;line-height:0;'>&nbsp;</td></tr><tr><td style='padding:32px;color:#1a2232;font-size:15px;line-height:1.6;'>"
            + content +
            "</td></tr><tr><td style='background:#f6f7fb;padding:20px 32px;font-size:12px;color:#6b7280;line-height:1.5;border-top:1px solid #e5e7ee;'>Barangay Manguikay, Mandaue City<br>This is an automatic message. Please do not reply.</td></tr></table></td></tr></table></body></html>";
    }
}