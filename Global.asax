﻿<%@ Application Language="C#" %>
<%@ Import Namespace="System.IO" %>
<%@ Import Namespace="ASP_Alt" %>
<%@ Import Namespace="System.Web.Optimization" %>
<%@ Import Namespace="System.Web.Routing" %>
<%@ Import Namespace="BusinessLayer" %>
<%@ Import Namespace="Common" %>
<%@ Import Namespace="CommonLogging" %>
<%@ Import Namespace="SecurityLayer" %>

<script runat="server">

    void Application_Start(object sender, EventArgs e)
    {
        RouteConfig.RegisterRoutes(RouteTable.Routes);
        BundleConfig.RegisterBundles(BundleTable.Bundles);

        // attach a logger instance to the Global application.
        // keep the logger, and target logging stream, open until the application ends.
        StreamWriter writer = new StreamWriter(
                Server.MapPath(GeneralConstants.LogFileDefaultLocation), true);
        writer.AutoFlush = true;
        Logger logger = new Logger(LoggingLevel.Debug, writer);
        logger.AppendDateTime = true;
        Application.Add(GeneralConstants.LoggerApplicationStateKey, logger);
    }

    void Session_Start(object sender, EventArgs e)
    {
        //Session[Security.SessionIdentifierLogin] = null;
        //Session[Security.SessionIdentifierSecurityToken] = null;
        Session[GeneralConstants.SessionCartItems] = new List<OrderItem>();
        Session[Security.SessionIdentifierLogin] = null;
        Session[Security.SessionIdentifierSecurityToken] = null;

        // TODO: remove this at Release.
        AdminController controller = new AdminController();
        List<Customer> customers = controller.GetCustomers();
        List<Cap> caps = controller.GetCaps();
        List<Colour> colours = controller.GetColours();
        if (customers.Count > 0)
        {
            Session[Security.SessionIdentifierLogin] = customers[0].Login;
            Session[Security.SessionIdentifierSecurityToken] = Security.GenerateSecurityTokenHash(customers[0].Login, customers[0].Password);
        }

        // TODO: Remove this at Release
        OrderItem o = new OrderItem {Cap = caps[0], Colour = colours[0]};
        o.CapId = o.Cap.ID;
        o.ColourId = o.Colour.ID;
        o.Quantity = 2;
        ((List<OrderItem>) Session[GeneralConstants.SessionCartItems]).Add(o);
        o = new OrderItem {Cap = caps[1], Colour = colours[3]};
        o.CapId = o.Cap.ID;
        o.ColourId = o.Colour.ID;
        o.Quantity = 1;
        ((List<OrderItem>) Session[GeneralConstants.SessionCartItems]).Add(o);

    }

    void Session_End(object sender, EventArgs e)
    {
        Session[Security.SessionIdentifierLogin] = null;
        Session[Security.SessionIdentifierSecurityToken] = null;
        if (Session[GeneralConstants.SessionCartItems] != null)
        {
            (Session[GeneralConstants.SessionCartItems] as List<OrderItem>).Clear();
        }
    }

    // TODO: remove this when finished development
    void Application_End(object sender, EventArgs e)
    {
        Session.Abandon();
    }

</script>
