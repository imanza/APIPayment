<%@ Page Language="C#" CodeBehind="" Inherits="System.Web.Mvc.ViewPage<dynamic>" %>

<!DOCTYPE html>

<html>
<head runat="server">
    <meta name="viewport" content="width=device-width" />
    <title>SalesValidation</title>
    <script src="../../Scripts/angular.min.js"></script>
    <link href="../../Styles/master.css" rel="stylesheet" />
    <link href="../../Styles/all-content-sec.css" rel="stylesheet" />
    <script src="../../Scripts/jquery-2.1.1.js"></script>
    <script src="../../Scripts/jquery-ui-1.10.4.min.js"></script>
    <script src="../../Scripts/master-Jquery/js-ajax-request.js"></script>
</head>
<body>
     <div class="ajax-loader">
            <img alt="loader" src="Images/Loader.gif" />
        </div>
        <div id="bodycontent">
            <section id="main" class="ajax-content">
                <div class="ajaxContainer"></div>
                <div id="ajaxBody" class="content-body">
                    <header class="header">
                    </header>
                    <div class="base-content">
                        <div class="signin-content">
                            <h1>Sign in
                            </h1>
                            <div class="ele-content">
                                <label>Email:</label>
                                <input id="txtUsername" type="text" />
                            </div>
                            <div class="ele-content">
                                <label>PASSWORD:</label>
                                <input id="txtPassword" type="password" />
                            </div>
                            <div class="ele-content">
                                <button id="bttnSubmit" type="button">Sign in</button>
                            </div>
                            <div class="ele-content account">
                                <a title="Create Account" href="/register">Sign Up Now</a>
                            </div>
                        </div>
                    </div>
                </div>
            </section>
        </div>
</body>
</html>
