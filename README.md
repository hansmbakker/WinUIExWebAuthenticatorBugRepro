# Bug repro

This contains a Maui bug repro app for [WinUIEx `WebAuthenticator`](https://github.com/dotMorten/WinUIEx/blob/main/src/WinUIEx/WebAuthenticator.cs).

The app is based on the Maui app template from Visual Studio.

It uses the Philips Hue Remote API as a test backend. The clientId / clientSecret work and are created solely for this repro purpose. The app will be deleted after this repro is not needed anymore.

Variants:

- ❌when referencing the WinUIEx 2.1 nuget package, the app activation is not redirected to the original instance where the user started the login flow. This breaks the login flow.
- ✅when the relevant source files from the WinUIEx GitHub repo are included and the nuget package is excluded, the activation redirection works properly.

To test:

- Click the "login" button on the MainPage
- Sign in on Philips Hue page using Philips Hue remote account
- Browser is redirected to approval page (still Philips Hue browser flow)
- Upon giving approval for the bridge, the browser will ask if it may open the callback uri (`bugrepro://huecallback`)
- When clicking ok, one of the variants happens based on configuration in the csproj file:

```xml
<!--Comment both ItemGroups and the nuget package to compare-->
<ItemGroup>
    <Compile Remove="WinUIEx\Helpers.cs" />
    <Compile Remove="WinUIEx\WebAuthenticator.cs" />
    <Compile Remove="WinUIEx\WebAuthenticatorResult.cs" />
</ItemGroup>

<ItemGroup>
    <None Include="WinUIEx\Helpers.cs" />
    <None Include="WinUIEx\WebAuthenticator.cs" />
    <None Include="WinUIEx\WebAuthenticatorResult.cs" />
</ItemGroup>

<ItemGroup Condition="'$(TargetFramework)' == 'net7.0-windows10.0.19041.0'">
    <PackageReference Include="WinUIEx">
    <Version>2.1.0</Version>
    </PackageReference>
</ItemGroup>
<!--End of what needs to be commented out-->
```
