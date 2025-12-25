<#import "template.ftl" as layout>
<@layout.registrationLayout displayMessage=!messagesPerField.existsError('username','password') displayInfo=realm.password && realm.registrationAllowed && !registrationDisabled??; section>
    <#if section = "header">
        <script>
            (function() {
                try {
                    const savedTheme = localStorage.getItem('kc-theme') || 'light';
                    if (savedTheme === 'dark') {
                        document.documentElement.classList.add('dark');
                    }
                } catch(e) {}
            })();
        </script>
        <div id="kc-header">
            <#if properties.logo??>
                <img src="${properties.logo}" alt="Assistente Executivo" />
            </#if>
            <h1>${msg("loginTitle", "Bem-vindo ao Assistente Executivo")}</h1>
        </div>
        <div id="kc-locale-wrapper">
            <#if locale??>
                <div id="kc-locale">
                    <div id="kc-locale-dropdown">
                        <a href="#" id="kc-current-locale-link" onclick="return false;">
                            <#if locale.current??>${locale.current}<#else>${locale.currentLanguageTag!}</#if>
                        </a>
                        <ul id="kc-locale-dropdown-list">
                            <#if locale.supported??>
                                <#list locale.supported as l>
                                    <li class="kc-locale-item <#if locale.currentLanguageTag == l.languageTag>kc-locale-item-selected</#if>">
                                        <a href="${l.url}">${l.label}</a>
                                    </li>
                                </#list>
                            </#if>
                        </ul>
                    </div>
                </div>
            </#if>
            <button id="theme-toggle" class="theme-toggle" aria-label="Toggle theme">
                <span class="theme-icon-light">☀️</span>
                <span class="theme-icon-dark">🌙</span>
            </button>
        </div>
    <#elseif section = "form">
        <#if social.providers??>
            <div id="kc-social-providers">
                <#list social.providers as p>
                    <a id="social-${p.alias}" class="btn btn-social btn-${p.alias}" href="${p.loginUrl}">
                        <#if p.alias == "google">
                            <svg class="social-icon" viewBox="0 0 24 24" width="20" height="20">
                                <path fill="#4285F4" d="M22.56 12.25c0-.78-.07-1.53-.2-2.25H12v4.26h5.92c-.26 1.37-1.04 2.53-2.21 3.31v2.77h3.57c2.08-1.92 3.28-4.74 3.28-8.09z"/>
                                <path fill="#34A853" d="M12 23c2.97 0 5.46-.98 7.28-2.66l-3.57-2.77c-.98.66-2.23 1.06-3.71 1.06-2.86 0-5.29-1.93-6.16-4.53H2.18v2.84C3.99 20.53 7.7 23 12 23z"/>
                                <path fill="#FBBC05" d="M5.84 14.09c-.22-.66-.35-1.36-.35-2.09s.13-1.43.35-2.09V7.07H2.18C1.43 8.55 1 10.22 1 12s.43 3.45 1.18 4.93l2.85-2.22.81-.62z"/>
                                <path fill="#EA4335" d="M12 5.38c1.62 0 3.06.56 4.21 1.64l3.15-3.15C17.45 2.09 14.97 1 12 1 7.7 1 3.99 3.47 2.18 7.07l3.66 2.84c.87-2.6 3.3-4.53 6.16-4.53z"/>
                            </svg>
                            ${msg("doLoginWith", "Entrar com")} Google
                        <#else>
                            ${p.displayName!p.alias}
                        </#if>
                    </a>
                </#list>
                <div class="divider">
                    <span>${msg("or", "ou")}</span>
                </div>
            </div>
        </#if>
        <div id="kc-form">
            <div id="kc-form-wrapper">
                <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
                    <div class="form-group">
                        <label for="username" class="${properties.kcLabelClass!}">
                            <#if !realm.loginWithEmailAllowed>
                                ${msg("username")}
                            <#elseif !realm.registrationEmailAsUsername>
                                ${msg("usernameOrEmail")}
                            <#else>
                                ${msg("email")}
                            </#if>
                        </label>
                        <input tabindex="1" id="username" class="form-control ${properties.kcInputClass!}" 
                               name="username" value="${(login.username!'')}" type="text" autofocus autocomplete="off"
                               aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                               placeholder="<#if !realm.loginWithEmailAllowed>${msg("username")}<#elseif !realm.registrationEmailAsUsername>${msg("usernameOrEmail")}<#else>${msg("email")}</#if>"/>
                        <#if messagesPerField.existsError('username','password')>
                            <span id="input-error" aria-live="polite" class="alert-error">
                                ${kcSanitize(messagesPerField.getFirstError('username','password'))?no_esc}
                            </span>
                        </#if>
                    </div>
                    
                    <div class="form-group">
                        <label for="password" class="${properties.kcLabelClass!}">${msg("password")}</label>
                        <input tabindex="2" id="password" class="form-control ${properties.kcInputClass!}" 
                               name="password" type="password" autocomplete="off"
                               aria-invalid="<#if messagesPerField.existsError('username','password')>true</#if>"
                               placeholder="${msg("password")}"/>
                    </div>
                    
                    <div class="form-options">
                        <#if realm.rememberMe && !usernameEdit??>
                            <div class="checkbox">
                                <label>
                                    <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox" checked> 
                                    ${msg("rememberMe")}
                                </label>
                            </div>
                        </#if>
                        <#if realm.resetPasswordAllowed>
                            <span><a tabindex="5" href="${url.loginResetCredentialsUrl}">${msg("doForgotPassword")}</a></span>
                        </#if>
                    </div>
                    
                    <div id="kc-form-buttons" class="form-group">
                        <input type="hidden" id="id-hidden-input" name="credentialId" 
                               <#if auth.selectedCredential?has_content>value="${auth.selectedCredential}"</#if>/>
                        <input tabindex="4" class="btn btn-primary btn-block" name="login" id="kc-login" 
                               type="submit" value="${msg("doLogIn")}"/>
                    </div>
                </form>
            </div>
        </div>
        
    <#elseif section = "info">
        <#if realm.password && realm.registrationAllowed && !registrationDisabled??>
            <div id="kc-registration-container">
                <div id="kc-registration">
                    <span class="text-secondary">${msg("noAccount")} </span>
                    <a href="${url.registrationUrl}">${msg("doRegister")}</a>
                </div>
            </div>
        </#if>
    </#if>
</@layout.registrationLayout>

<script src="${url.resourcesPath}/js/theme.js"></script>
<script src="${url.resourcesPath}/js/locale.js"></script>

