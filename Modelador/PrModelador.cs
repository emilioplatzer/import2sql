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
			[Fk] public Empresas fkEmpresas;
		}
		class PartesProductos:Tabla{
			[Pk] public CampoEntero cEmpresa;
			[Pk] public CampoProducto cProducto;
			[Pk] public CampoEntero cParte;
			public CampoNombre cNombreParte;
			public CampoEntero cCantidad;
			[Fk] public Productos fkProductos;
		}
		[Test]
		public void Periodos(){
			Periodos p=new Periodos();
			Assert.AreEqual(0,p.cAno.Valor);
			Assert.AreEqual("Ano",p.cAno.Nombre);
			Assert.AreEqual("create table periodos(ano integer,mes integer,anoant integer,mesant integer,primary key(ano,mes));"
			                ,Cadena.Simplificar(p.SentenciaCreateTable()));
			Productos pr=new Productos();
			Assert.AreEqual("create table productos(empresa integer,producto varchar(4),nombreproducto varchar(250),primary key(empresa,producto),foreign key(empresa)references empresas(empresa));"
			                ,Cadena.Simplificar(pr.SentenciaCreateTable()));
		}
		[Test]
		public void SentenciaInsert(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("INSERT INTO [productos] ([producto], [nombreproducto]) SELECT p.[producto], p.[producto] AS [nombreproducto]\n FROM [productos] p;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(new Productos()).Select(p.cProducto,p.cNombreProducto.Es(p.cProducto))));
		}
		[Test]
		public void SentenciaUpdate(){
			Productos p=new Productos();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("UPDATE [productos] SET [producto]='P1', [nombreproducto]='Producto 1';\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cProducto.Set("P1"),p.cNombreProducto.Set("Producto 1"))));
			string Esperado="UPDATE [productos] SET [producto]='P1', [nombreproducto]='Producto 1'\n WHERE [producto]='P3'\n AND ([nombreproducto] IS NULL OR [nombreproducto]<>[producto])";
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
			Empresas e=new Empresas();
			e.cEmpresa.Valor=13;
			CampoDestino<int> cCantidadPartes=new CampoDestino<int>("cantidadpartes");
			using(Ejecutador ej=new Ejecutador(dba,e)){
				Sentencia s=
					new SentenciaSelect(e.cEmpresa,e.cNombreEmpresa);
				Assert.AreEqual("SELECT e.[empresa], e.[nombreempresa]\n FROM [empresas] e\n WHERE e.[empresa]=13;\n",
				                ej.Dump(s));
				PartesProductos pp=new PartesProductos(); 
				pp.UsarFk();
				Productos pr=pp.fkProductos;
				Sentencia s2=
					new SentenciaSelect(pr.cProducto,pr.cNombreProducto,cCantidadPartes.EsSuma(pp.cCantidad));
				Assert.AreEqual("SELECT p.[producto], p.[nombreproducto], SUM(pa.[cantidad]) AS [cantidadpartes]\n FROM [productos] p, [partesproductos] pa\n WHERE p.[empresa]=pa.[empresa]\n AND p.[producto]=pa.[producto]\n AND p.[empresa]=13\n AND pa.[empresa]=13;\n"
				                ,ej.Dump(s2));
			}
		}
	}
}
