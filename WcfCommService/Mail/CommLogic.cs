using System;
using System.Collections.Generic;
using System.Linq;

using System.Net;
using System.Net.Mail;

namespace WcfCommService
{
    // here is where the logic of the service sits
    public class CommLogic {
	
	private MailClient mailClient;

	public CommLogic(MailClient client)
	{
	    mailClient = client;
	}

	public HandleNewRequest(ServiceRequest request) 
	{
	    // construct a mail message from the service request

	    // send the mail message
	}
    }
}

