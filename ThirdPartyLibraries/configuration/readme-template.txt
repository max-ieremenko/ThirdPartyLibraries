﻿Licenses
--------
	
|Code|Requires approval|Requires third party notices|Packages count|
|----------|:----|:----|:----|
{% for license in Licenses -%}
|[{{license.Code}}]({{license.LocalHRef}})|{% if license.RequiresApproval%}yes{% else %}no{% endif %}|{% if license.RequiresThirdPartyNotices%}yes{% else %}no{% endif %}|{{license.PackagesCount}}|
{% endfor -%}

{%- if TodoPackagesCount > 0 -%}
TODO {{TodoPackagesCount}}
--------

|Name|Version|Source|License|Used by|
|----------|:----|:----|:----|:----|
{%- for package in TodoPackages -%}
|[{{package.Name}}]({{package.LocalHRef}})|{{package.Version}}|[{{package.Source}}]({{package.SourceHRef}})|{{package.LicenseMarkdownExpression}}|{{package.UsedBy}}|
{%- endfor -%}
{%- endif -%}


Packages {{PackagesCount}}
--------

|Name|Version|Source|License|Used by|
|----------|:----|:----|:----|:----|
{%- for package in Packages -%}
|[{{package.Name}}]({{package.LocalHRef}})|{{package.Version}}|[{{package.Source}}]({{package.SourceHRef}})|{{package.LicenseMarkdownExpression}}|{{package.UsedBy}}|
{%- endfor -%}

*This page was generated by a tool.*