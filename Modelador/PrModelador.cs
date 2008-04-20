/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 14/04/2008
 * Time: 09:56 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;

using Comunes;
using BasesDatos;
using Modelador;
using Indices;

namespace PrModelador
{
	public class Periodos:Tabla{
		[Pk] public CampoEntero cAno;
		[Pk] public CampoEntero cMes;
		public CampoEntero cAnoAnt;
		public CampoEntero cMesAnt;
	}
	[TestFixture]
	public class prTabla{
		#pragma warning disable 649
		public prTabla(){
		}
		class Empresas:Tabla{
			[Pk] public CampoEntero cEmpresa;
			public CampoNombre cNombreEmpresa;
		}
		class Productos:Tabla{
			[Pk] public CampoEntero cEmpresa;
			[Pk] public CampoProducto cProducto;
			public CampoNombre cNombreProducto;
			public CampoEntero cEstado;
			public CampoReal cCosto;
			[Fk] public Empresas fkEmpresas;
		}
		class PartesProductos:Tabla{
			[Pk] public CampoEntero cEmpresa;
			[Pk] public CampoProducto cProducto;
			[Pk] public CampoEntero cParte;
			public CampoNombre cNombreParte;
			public CampoEntero cCantidad;
			[FkMixta("ant")] public CampoEntero cParteAnterior;
			[Fk] public Productos fkProductos;
			[FkMixta("ant")] public PartesProductos fkParteAnterior;
		}
		class NovedadesProductos:Tabla{
			[Pk] public CampoEntero cEmpresa;
			[Pk] public CampoProducto cProductoAuxiliar;
			public CampoEntero cNuevoEstado;
		}
		class Numeros:Tabla{
			[Pk] public CampoEntero cNumero;
		}
		#pragma warning restore 649
		[Test]
		public void CreacionTablas(){
			BdAccess dba=BdAccess.SinAbrir();
			PostgreSql dbp=PostgreSql.SinAbrir();
			Periodos p=new Periodos();
			Assert.AreEqual(0,p.cAno.Valor);
			Assert.AreEqual("Ano",p.cAno.Nombre);
			Assert.AreEqual("create table periodos(ano integer,mes integer,anoant integer,mesant integer,primary key(ano,mes));"
			                ,Cadena.Simplificar(p.SentenciaCreateTable(dba)));
			Productos pr=new Productos();
			Assert.AreEqual("create table productos(empresa integer,producto varchar(4),nombreproducto varchar(250),estado integer,costo double precision,primary key(empresa,producto),foreign key(empresa)references empresas(empresa));"
			                ,Cadena.Simplificar(pr.SentenciaCreateTable(dba)));
			PartesProductos pa=new PartesProductos();
			Assert.AreEqual(false,dba.SoportaFkMixta);
			pa.UsarFk();
			Assert.AreEqual("partesproductos",pa.TablasFk[1].NombreTabla);
			Assert.AreEqual(Fk.Tipo.Mixta,pa.TablasFk[1].TipoFk);
			Assert.AreEqual("create table partesproductos(empresa integer,producto varchar(4),parte integer,nombreparte varchar(250),cantidad integer,parteanterior integer,primary key(empresa,producto,parte),foreign key(empresa,producto)references productos(empresa,producto));"
			                ,Cadena.Simplificar(pa.SentenciaCreateTable(dba)));
			Assert.AreEqual("create table partesproductos(empresa integer,producto varchar(4),parte integer,nombreparte varchar(250),cantidad integer,parteanterior integer,primary key(empresa,producto,parte),foreign key(empresa,producto)references productos(empresa,producto),foreign key(empresa,producto,parteanterior)references partesproductos(empresa,producto,parte));"
			                ,Cadena.Simplificar(pa.SentenciaCreateTable(dbp)));
		}
		[Test]
		public void SentenciaInsert(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("INSERT INTO productos (producto, nombreproducto) SELECT p.producto, p.producto AS nombreproducto\n FROM productos p;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(new Productos()).Select(p.cProducto,p.cNombreProducto.Es(p.cProducto))));
			PartesProductos pp=new PartesProductos();
			pp.UsarFk();
			Productos pr=pp.fkProductos;
			NovedadesProductos np=new NovedadesProductos();
			np.EsFkDe(pr,pr.cProducto);
			Assert.AreEqual("INSERT INTO partesproductos (producto, cantidad, nombreparte) SELECT p.producto, SUM(p.costo) AS cantidad, n.nuevoestado AS nombreparte\n FROM productos p, novedadesproductos n\n WHERE n.empresa=p.empresa\n AND n.productoauxiliar=p.producto\n GROUP BY p.producto, n.nuevoestado;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(pp).Select(pr.cProducto,pp.cCantidad.EsSuma(pr.cCosto),pp.cNombreParte.Es(np.cNuevoEstado))));
		}
		[Test]
		public void SentenciaUpdate(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			dba.TipoStuffActual=BaseDatos.TipoStuff.Siempre;
			Assert.AreEqual("UPDATE [productos] SET [producto]='P1',\n [nombreproducto]='Producto 1';\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"))));
			string Esperado="UPDATE [productos] SET [producto]='P1',\n [nombreproducto]='Producto 1'\n WHERE [producto]='P3'\n AND ([nombreproducto] IS NULL OR [nombreproducto]<>[producto])";
			Assert.AreEqual(Esperado+";\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"))
			                      .Where(p.cProducto.Igual("P3")
			                             .And(p.cNombreProducto.EsNulo()
			                                  .Or(p.cNombreProducto.Distinto(p.cProducto))))));
			SentenciaUpdate sentencia=new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"));
			sentencia.Where(p.cProducto.Igual("P3")
			                .And(p.cNombreProducto.EsNulo()
			                     .Or(p.cNombreProducto.Distinto(p.cProducto))));
			Assert.AreEqual(1,sentencia.Tablas().Count);
			Assert.AreEqual("productos",sentencia.Tablas()[0].NombreTabla);
			sentencia.Where(p.cNombreProducto.Distinto("P0"));
			Esperado+="\n AND [nombreproducto]<>'P0'";
			Assert.AreEqual(Esperado+";\n",new Ejecutador(dba).Dump(sentencia));
			Assert.AreEqual(Esperado+";\n",new Ejecutador(dba).Dump(sentencia));
			Empresas contexto=new Empresas();
			contexto.cEmpresa.AsignarValor(14);
			using(Ejecutador ej=new Ejecutador(dba,contexto)){
				Esperado+="\n AND [empresa]=14";
				Assert.AreEqual(Esperado+";\n",ej.Dump(sentencia));
			}
		}	
		[Test]
		public void SentenciaCompuesta(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			BaseDatos dbp=PostgreSql.SinAbrir();
			dba.TipoStuffActual=BaseDatos.TipoStuff.Siempre;
			Empresas e=new Empresas();
			e.cEmpresa.Valor=13;
			CampoDestino<int> cCantidadPartes=new CampoDestino<int>("cantidadpartes");
			using(Ejecutador ej=new Ejecutador(dba,e), ejp=new Ejecutador(dbp,e)){
				Sentencia s=
					new SentenciaSelect(e.cEmpresa,e.cNombreEmpresa);
				Assert.AreEqual("SELECT e.[empresa], e.[nombreempresa]\n FROM [empresas] e\n WHERE e.[empresa]=13;\n",
				                ej.Dump(s));
				PartesProductos pp=new PartesProductos(); 
				pp.UsarFk();
				Productos pr=pp.fkProductos;
				Assert.AreEqual(pp,pr.TablaRelacionada);
				Sentencia s2=
					new SentenciaSelect(pr.cProducto,pr.cNombreProducto,cCantidadPartes.EsSuma(pp.cCantidad));
				Assert.AreEqual("SELECT p.[producto], p.[nombreproducto], SUM(pa.[cantidad]) AS [cantidadpartes]\n FROM [productos] p, [partesproductos] pa\n WHERE p.[empresa]=pa.[empresa]\n AND p.[producto]=pa.[producto]\n AND p.[empresa]=13\n AND pa.[empresa]=13\n GROUP BY p.[producto], p.[nombreproducto];\n"
				                ,ej.Dump(s2));
				Assert.AreEqual("SELECT SUM(p.[cantidad]) AS [cantidadpartes]\n FROM [partesproductos] p, [productos] pr\n WHERE pr.[producto]<>pr.[nombreproducto]\n AND pr.[empresa]=p.[empresa]\n AND pr.[producto]=p.[producto]\n AND p.[empresa]=13\n AND pr.[empresa]=13;\n"
				                ,ej.Dump(new SentenciaSelect(cCantidadPartes.EsSuma(pp.cCantidad)).Where(pr.cProducto.Distinto(pr.cNombreProducto))));
				Sentencia su=
					new SentenciaUpdate(pp,pp.cNombreParte.Set(pr.cNombreProducto.Concatenado(pp.cParte))).Where(pp.cNombreParte.EsNulo());
				Assert.AreEqual("UPDATE [partesproductos] p INNER JOIN [productos] pr ON p.[empresa]=pr.[empresa] AND p.[producto]=pr.[producto]\n " +
				                "SET p.[nombreparte]=(pr.[nombreproducto] & p.[parte])\n WHERE p.[nombreparte] IS NULL\n AND p.[empresa]=13\n AND pr.[empresa]=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE [partesproductos] p INNER JOIN [productos] pr ON p.[empresa]=pr.[empresa] AND p.[producto]=pr.[producto]\n " +
				                "SET p.[nombreparte]=(pr.[nombreproducto] & p.[parte])\n WHERE p.[nombreparte] IS NULL\n AND p.[empresa]=13\n AND pr.[empresa]=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE partesproductos " +
				                "SET nombreparte=(SELECT (pr.nombreproducto||parte) FROM productos pr WHERE pr.empresa=empresa AND pr.producto=producto)\n WHERE nombreparte IS NULL\n AND empresa=13;\n",
				                ejp.Dump(su));
				dba.TipoStuffActual=BaseDatos.TipoStuff.Inteligente;
				su=new SentenciaUpdate(pr,pr.cEstado.Set(0)).Where(pr.cEstado.EsNulo());
				Assert.AreEqual("UPDATE productos SET estado=0\n WHERE estado IS NULL\n AND empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE productos SET estado=0\n WHERE estado IS NULL\n AND empresa=13;\n",
				                ejp.Dump(su));
				su=new SentenciaUpdate(pp,pp.cCantidad.Set(pp.cCantidad.Mas(1))).Where(pp.cCantidad.EsNulo());
				Assert.AreEqual("UPDATE partesproductos SET cantidad=cantidad+1\n WHERE cantidad IS NULL\n AND empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE partesproductos SET cantidad=cantidad+1\n WHERE cantidad IS NULL\n AND empresa=13;\n",
				                ejp.Dump(su));
				su=new SentenciaSelect(p.cProducto,p.cNombreProducto,pr.cNombreProducto).Where(p.cProducto.Igual(pr.cProducto.Concatenado("2")));
				Assert.AreEqual("SELECT p.producto, p.nombreproducto, pr.nombreproducto\n FROM productos p, productos pr\n WHERE p.producto=(pr.producto & '2')\n AND p.empresa=13\n AND pr.empresa=13;\n",
				                ej.Dump(su));
				NovedadesProductos np=new NovedadesProductos();
				np.EsFkDe(pr,pr.cProducto);
				pr.LiberadaDelContextoDelEjecutador=true;
				Assert.AreEqual(pr,np.TablaRelacionada);
				su=new SentenciaUpdate(pr,pr.cEstado.Set(np.cNuevoEstado),pr.cNombreProducto.Set(pr.cNombreProducto.Concatenado(np.cNuevoEstado))).Where(pr.cProducto.Distinto("P_este"));
				Assert.AreEqual("UPDATE productos p INNER JOIN novedadesproductos n ON p.empresa=n.empresa AND p.producto=n.productoauxiliar\n" +
				                " SET p.estado=n.nuevoestado,\n p.nombreproducto=(p.nombreproducto & n.nuevoestado)\n" +
				                " WHERE p.producto<>'P_este'\n AND n.empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE productos " +
				                "SET estado=(SELECT n.nuevoestado FROM novedadesproductos n WHERE n.empresa=empresa AND n.productoauxiliar=producto),\n" +
				                " nombreproducto=(SELECT (nombreproducto||n.nuevoestado) FROM novedadesproductos n WHERE n.empresa=empresa AND n.productoauxiliar=producto)\n" +
				                " WHERE producto<>'P_este';\n",
				                ejp.Dump(su));
				su=new SentenciaUpdate(pr,pr.cEstado.Set(np.cNuevoEstado),pr.cCosto.SetNull()).Where(pr.cProducto.Distinto("P_este"));
				Assert.AreEqual("UPDATE productos p INNER JOIN novedadesproductos n ON p.empresa=n.empresa AND p.producto=n.productoauxiliar\n SET p.estado=n.nuevoestado,\n p.costo=null\n WHERE p.producto<>'P_este'\n AND n.empresa=13;\n",
				                ej.Dump(su));
				
				/* Falta programar
				Assert.AreEqual("UPDATE productos SET estado=(SELECT n.nuevoestado FROM novedadesproductos n WHERE n.empresa=empresa AND n.productoauxiliar=producto),\n p.costo=null\n WHERE p.producto<>'P_este'\n AND p.empresa=13\n;\n",
				                ejp.Dump(su));
				*/
			}
		}
		[Test]
		public void MultiFk(){
			BaseDatos dba=BdAccess.SinAbrir();
			BaseDatos dbp=PostgreSql.SinAbrir();
			Productos p=new Productos();
			Numeros num=new Numeros();
			{
				PartesProductos pp=new PartesProductos();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero));
				Sentencia s=
					new SentenciaSelect(p.cProducto,p.cNombreProducto,num.cNumero,pp.cNombreParte).Where(num.cNumero.Operado("<=",3));
				Assert.AreEqual("SELECT p.producto, p.nombreproducto, n.numero, pa.nombreparte\n" +
				                " FROM productos p, numeros n, partesproductos pa\n" +
				                " WHERE n.numero<=3\n AND pa.empresa=p.empresa\n AND pa.producto=p.producto\n AND pa.parte=n.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesProductos pp=new PartesProductos();
				pp.EsFkDe(p,pp.cParte.Es(7));
				Sentencia s=
					new SentenciaSelect(p.cProducto,p.cNombreProducto,pp.cNombreParte);
				Assert.AreEqual("SELECT p.producto, p.nombreproducto, pa.nombreparte\n" +
				                " FROM productos p, partesproductos pa\n" +
				                " WHERE pa.empresa=p.empresa\n AND pa.producto=p.producto\n AND pa.parte=7;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesProductos pp=new PartesProductos();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero),pp.cEmpresa.Es(1));
				Sentencia s=
					new SentenciaSelect(p.cProducto,p.cNombreProducto,num.cNumero,pp.cNombreParte);
				Assert.AreEqual("SELECT p.producto, p.nombreproducto, n.numero, pa.nombreparte\n" +
				                " FROM productos p, numeros n, partesproductos pa\n" +
				                " WHERE pa.empresa=1\n AND pa.producto=p.producto\n AND pa.parte=n.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesProductos pp=new PartesProductos();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero),pp.cEmpresa.Es(1));
				Sentencia s=
					new SentenciaSelect(p.cProducto,p.cNombreProducto,pp.cNombreParte);
				Assert.Ignore("Falta considerar tablas que sirvan para los joins y no estén en los campos del select");
				Assert.AreEqual("SELECT p.producto, p.nombreproducto, pa.nombreparte\n" +
				                " FROM productos p, numeros n, partesproductos pa\n" +
				                " WHERE pa.empresa=1\n AND pa.producto=p.producto\n AND pa.parte=n.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
		}
		[Test]
		public void UpdateSuma(){
			BaseDatos dba=BdAccess.SinAbrir();
			BaseDatos dbp=PostgreSql.SinAbrir();
			PartesProductos pp=new PartesProductos();
			pp.UsarFk();
			Productos pr=pp.fkProductos;
			Assert.AreEqual(pr.TablaRelacionada,pp);
			SentenciaUpdate su=
				new SentenciaUpdate(pr,pr.cCosto.Set(pr.SelectSuma(pp.cCantidad)));
			Assert.AreEqual("UPDATE productos SET costo=DSum('cantidad','partesproductos','empresa=''' & empresa & ''' AND producto=''' & producto & '''');\n"
			                ,new Ejecutador(dba).Dump(su));
			Assert.AreEqual("UPDATE productos SET costo=(SELECT SUM(pa.cantidad) FROM partesproductos pa WHERE pa.empresa=empresa AND pa.producto=producto);\n"
			                ,new Ejecutador(dbp).Dump(su));
			NovedadesProductos np=new NovedadesProductos();
			np.UsarFk();
			pr.EsFkDe(np,np.cProductoAuxiliar);
			su=new SentenciaUpdate(pr,pr.cCosto.Set(pr.SelectSuma(np.cNuevoEstado)));
			Assert.AreEqual("UPDATE productos SET costo=DSum('nuevoestado','novedadesproductos','empresa=''' & empresa & ''' AND productoauxiliar=''' & producto & '''');\n"
			                ,new Ejecutador(dba).Dump(su));
			Assert.AreEqual("UPDATE productos SET costo=(SELECT SUM(n.nuevoestado) FROM novedadesproductos n WHERE n.empresa=empresa AND n.productoauxiliar=producto);\n"
			                ,new Ejecutador(dbp).Dump(su));
		}
	}
}
