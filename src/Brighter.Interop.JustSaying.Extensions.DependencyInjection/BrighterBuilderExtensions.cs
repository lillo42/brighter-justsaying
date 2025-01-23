using System.Globalization;
using System.Reflection;

using Brighter.Interop.JustSaying;
using Brighter.Interop.JustSaying.Mappers;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

using Namotion.Reflection;

using Paramore.Brighter.Extensions.DependencyInjection;

namespace Paramore.Brighter.Extensions.DependencyInjection;

/// <summary>
/// The <see cref="IBrighterBuilder"/> extensions method, to support brighter and just saying interops
/// </summary>
public static class BrighterBuilderExtensions
{
    /// <summary>
    /// Automatic register the <see cref="JustSayingMapper{T}"/> to any class that inheritance from <see cref="JustSayingMessage"/> 
    /// </summary>
    /// <param name="builder">The <see cref="IBrighterBuilder"/>.</param>
    /// <param name="assemblies">The extra assembly that we should look into.</param>
    /// <returns>
    /// The provided <paramref name="builder"/> with <see cref="JustSayingMapper{T}"/> register.
    /// </returns>
    public static IBrighterBuilder AutoRegisterJustSayingMapper(this IBrighterBuilder builder,
        params Assembly[] assemblies) => AutoRegisterJustSayingMapper(builder, null, assemblies);

    /// <summary>
    /// Automatic register the <see cref="JustSayingMapper{T}"/> to any class that inheritance from <see cref="JustSayingMessage"/> 
    /// </summary>
    /// <param name="builder">The <see cref="IBrighterBuilder"/>.</param>
    /// <param name="defaultMapperOptions">The action to configure <see cref="JustSayingMapperConfiguration"/>.</param>
    /// <param name="assemblies">The extra assembly that we should look into.</param>
    /// <returns>
    /// The provided <paramref name="builder"/> with <see cref="JustSayingMapper{T}"/> register.
    /// </returns>
    public static IBrighterBuilder AutoRegisterJustSayingMapper(this IBrighterBuilder builder, 
        Action<JustSayingMapperConfiguration>? defaultMapperOptions,
        Assembly[] assemblies)
    {
        var appDomainAssemblies = AppDomain.CurrentDomain.GetAssemblies().Where(a =>
            !a.IsDynamic && !a.FullName!.StartsWith("Microsoft.", true, CultureInfo.InvariantCulture) &&
            !a.FullName.StartsWith("System.", true, CultureInfo.InvariantCulture));

        assemblies = appDomainAssemblies.Concat(assemblies).ToArray();

        builder.Services.Configure<JustSayingMapperConfiguration>(opt =>
        {
            if (defaultMapperOptions != null)
            {
                defaultMapperOptions(opt);
            }
        });

        builder.MapperRegistry(services =>
        {
            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    if (type is { IsClass: true, IsAbstract: false } && type.IsAssignableTo(typeof(IJustSayingMessage)))
                    {
                        services.Add(type, typeof(JustSayingMapper<>).MakeGenericType(type));
                    }
                }
            }
        });

        return builder;
    }
}

#if NETSTANDARD2_0
internal static class TypeExtensions
{
    internal static bool IsAssignableTo(this Type type, Type? targetType) => targetType?.IsAssignableFrom(type) ?? false;
}
#endif