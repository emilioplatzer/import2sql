/*
 * Created by SharpDevelop.
 * User: Administrador
 * Date: 03/05/2008
 * Time: 11:08 a.m.
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using NUnit.Framework;

using Comunes;
using BasesDatos;
using ModeladorSql;

namespace PrModeladorSql
{
	public class CampoProducto:CampoChar{ public CampoProducto():base(4){} };
	public class CampoNombre:CampoChar{ public CampoNombre():base(250){} };
	#pragma warning disable 649
	[Alias("pe")]
	public class Periodos:Tabla{
		[Pk] public CampoEntero cAno;
		[Pk] public CampoEntero cMes;
		public CampoEnteroOpcional cAnoAnt;
		public CampoEnteroOpcional cMesAnt;
	}
	[Alias("e")]
	public class Empresas:Tabla{
		[Pk] public CampoEntero cEmpresa;
		public CampoNombre cNombreEmpresa;
	}
	[Alias("p")]
	public class Piezas:Tabla{
		[Pk] public CampoEntero cEmpresa;
		[Pk] public CampoPieza cPieza;
		public CampoNombre cNombrePieza;
		public CampoEntero cEstado;
		public CampoRealOpcional cCosto;
		[Fk] public Empresas fkEmpresas;
	}
	[Alias("pp")]
	public class PartesPiezas:Tabla{
		[Pk] public CampoEntero cEmpresa;
		[Pk] public CampoPieza cPieza;
		[Pk] public CampoEntero cParte;
		public CampoNombre cNombreParte;
		public CampoEnteroOpcional cCantidad;
		[Fk] public Empresas fkEmpresas;
		[FkMixta("ant")] public CampoEnteroOpcional cParteAnterior;
		[Fk] public Piezas fkPiezas;
		[FkMixta("ant")] public PartesPiezas fkParteAnterior;
	}
	[Alias("np")]
	public class NovedadesPiezas:Tabla{
		[Pk] public CampoEntero cEmpresa;
		[Pk] public CampoPieza cPiezaAuxiliar;
		public CampoEnteroOpcional cNuevoEstado;
	}
	public class Numeros:Tabla{
		[Pk] public CampoEntero cNumero;
	}
	[Alias("cc")]
	public class ColoresCompuestos:Tabla{
		public enum ColorPrimario{Rojo,Verde,Azul};
		[Pk] public CampoEntero cColorCompuesto;
		public CampoEnumerado<ColorPrimario> cColorBase;
	}
	#pragma warning restore 649
	public class CampoPieza:CampoProducto{};
	[TestFixture]
	public class prModelador{
		[Test]
		public void CreacionTablas(){
			BdAccess dba=BdAccess.SinAbrir();
			PostgreSql dbp=PostgreSql.SinAbrir();
			Periodos p=new Periodos();
			Assert.AreEqual(0,p.cAno.Valor);
			Assert.AreEqual("Ano",p.cAno.Nombre);
			Assert.AreEqual("CREATE TABLE periodos(ano INTEGER NOT NULL,mes INTEGER NOT NULL,anoant INTEGER,mesant INTEGER,PRIMARY KEY(ano,mes));"
			                ,Cadena.Simplificar(p.SentenciaCreateTable(dba)));
			Piezas pr=new Piezas();
			System.Console.WriteLine(pr.SentenciaCreateTable(dba));
			Assert.AreEqual("CREATE TABLE piezas(empresa INTEGER NOT NULL,pieza VARCHAR(4),nombrepieza VARCHAR(250),estado INTEGER NOT NULL,costo DOUBLE PRECISION,PRIMARY KEY(empresa,pieza),FOREIGN KEY(empresa)REFERENCES empresas(empresa));"
			                ,Cadena.Simplificar(pr.SentenciaCreateTable(dba)));
			PartesPiezas pa=new PartesPiezas();
			Assert.AreEqual(false,dba.SoportaFkMixta);
			pa.UsarFk();
			Assert.AreEqual("partespiezas",pa.TablasFk[2].NombreTabla);
			Assert.AreEqual(Fk.Tipo.Mixta,pa.TablasFk[2].TipoFk);
			Assert.AreEqual("CREATE TABLE partespiezas(empresa INTEGER NOT NULL,pieza VARCHAR(4),parte INTEGER NOT NULL,nombreparte VARCHAR(250),cantidad INTEGER,parteanterior INTEGER,PRIMARY KEY(empresa,pieza,parte),FOREIGN KEY(empresa)REFERENCES empresas(empresa),FOREIGN KEY(empresa,pieza)REFERENCES piezas(empresa,pieza));"
			                ,Cadena.Simplificar(pa.SentenciaCreateTable(dba)));
			Assert.AreEqual("CREATE TABLE partespiezas(empresa INTEGER NOT NULL,pieza VARCHAR(4),parte INTEGER NOT NULL,nombreparte VARCHAR(250),cantidad INTEGER,parteanterior INTEGER,PRIMARY KEY(empresa,pieza,parte),FOREIGN KEY(empresa)REFERENCES empresas(empresa),FOREIGN KEY(empresa,pieza)REFERENCES piezas(empresa,pieza),FOREIGN KEY(empresa,pieza,parteanterior)REFERENCES partespiezas(empresa,pieza,parte));"
			                ,Cadena.Simplificar(pa.SentenciaCreateTable(dbp)));
			var np=new NovedadesPiezas();
			Ejecutador.bitacora.Registrar(
				p.SentenciaCreateTable(dbp)+
				pr.SentenciaCreateTable(dbp)+
				pa.SentenciaCreateTable(dbp)+
				np.SentenciaCreateTable(dbp)
			);
		}
		[Test]
		public void SentenciaInsert(){
			Piezas p=new Piezas();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("INSERT INTO piezas (pieza, nombrepieza)\n SELECT p.pieza, p.pieza AS nombrepieza\n FROM piezas p;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(new Piezas()).Select(p.cPieza,p.cNombrePieza.Es(p.cPieza))));
			PartesPiezas pp=new PartesPiezas();
			pp.UsarFk();
			Piezas pr=pp.fkPiezas;
			NovedadesPiezas np=new NovedadesPiezas();
			np.EsFkDe(pr,pr.cPieza);
			Assert.AreEqual("INSERT INTO partespiezas (pieza, cantidad, nombreparte)\n " +
			                "SELECT p.pieza, SUM(p.estado) AS cantidad,\n STR(np.nuevoestado) AS nombreparte\n " +
			                "FROM piezas p, novedadespiezas np\n " +
			                "WHERE np.empresa=p.empresa\n AND np.piezaauxiliar=p.pieza\n " +
			                "GROUP BY p.pieza, STR(np.nuevoestado);\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(pp).Select(pr.cPieza,pp.cCantidad.EsSuma(pr.cEstado),pp.cNombreParte.Es(np.cNuevoEstado.NumeroACadena()))));
			Assert.AreEqual("INSERT INTO partespiezas (pieza, cantidad, nombreparte)\n " +
			                "SELECT p.pieza, SUM(p.costo) AS cantidad,\n STR(np.nuevoestado) AS nombreparte\n " +
			                "FROM piezas p, novedadespiezas np\n " +
			                "WHERE np.empresa=p.empresa\n AND np.piezaauxiliar=p.pieza\n " +
			                "GROUP BY p.pieza, STR(np.nuevoestado)\n " +
			                "HAVING COUNT(*)>=2\n AND COUNT(p.costo)>=1;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(pp)
				      .Select(pr.cPieza,pp.cCantidad.EsSuma(pr.cCosto),pp.cNombreParte.Es(np.cNuevoEstado.NumeroACadena()))
				      .Having(new CampoDestino<int>("cantidad_registros").EsCount().MayorOIgual(2),
				              new CampoDestino<int>("suma_costos").EsCount(pr.cCosto).MayorOIgual(1))
				     )
			);
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza, cantidad, nombreparte)\n SELECT 1 AS empresa, p.pieza, SUM(p.costo) AS cantidad,\n STR(np.nuevoestado) AS nombreparte\n FROM piezas p, novedadespiezas np\n WHERE np.empresa=p.empresa\n AND np.piezaauxiliar=p.pieza\n GROUP BY p.pieza, STR(np.nuevoestado);\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(pp).Select(pp.cEmpresa.Es(1),pr.cPieza,pp.cCantidad.EsSuma(pr.cCosto),pp.cNombreParte.Es(np.cNuevoEstado.NumeroACadena()))));
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza)\n VALUES (1, 'PROD1');\n",
			    new Ejecutador(dba)
			    .Dump(new SentenciaInsert(pp).Valores(pp.cEmpresa.Es(1),pr.cPieza.Es("PROD1"))));
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza, parte)\n SELECT p.empresa, p.pieza, 1 AS parte\n FROM piezas p;\n",
                new Ejecutador(dba)
                .Dump(new SentenciaInsert(pp).Select(p,pp.cParte.Es(1))));
			p.UsarFk();
			Empresas e=p.fkEmpresas;
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza, parte)\n SELECT e.empresa, p.pieza, 1 AS parte\n FROM empresas e, piezas p\n WHERE e.empresa=p.empresa;\n",
                new Ejecutador(dba)
                .Dump(new SentenciaInsert(pp).Select(e,p,pp.cParte.Es(1))));
		}
		[Test]
		public void SentenciaUpdate(){
			Piezas p=new Piezas();
			BaseDatos dba=BdAccess.SinAbrir();
			dba.TipoStuffActual=BaseDatos.TipoStuff.Siempre;
			Assert.AreEqual("UPDATE [piezas] p\n SET p.[pieza]='P1', p.[nombrepieza]='Pieza 1';\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cPieza.Es("P1"),p.cNombrePieza.Es("Pieza 1"))));
			string Esperado="UPDATE [piezas] p\n SET p.[pieza]='P1', p.[nombrepieza]='Pieza 1'\n WHERE p.[pieza]='P3'\n AND (p.[nombrepieza] IS NULL OR p.[nombrepieza]<>p.[pieza])";
			Assert.AreEqual(Esperado+";\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cPieza.Es("P1"),p.cNombrePieza.Es("Pieza 1"))
			                      .Where(p.cPieza.Igual("P3")
			                             .And(p.cNombrePieza.EsNulo()
			                                  .Or(p.cNombrePieza.Distinto(p.cPieza))))));
			Assert.AreEqual(Esperado+";\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cPieza.Es("P1"),p.cNombrePieza.Es("Pieza 1"))
			                      .Where(p.cPieza.Igual("P3"),
			                             p.cNombrePieza.EsNulo().Or(p.cNombrePieza.Distinto(p.cPieza)))));
			SentenciaUpdate sentencia=new SentenciaUpdate(p,p.cPieza.Es("P1"),p.cNombrePieza.Es("Pieza 1"));
			sentencia.Where(p.cPieza.Igual("P3")
			                .And(p.cNombrePieza.EsNulo()
			                     .Or(p.cNombrePieza.Distinto(p.cPieza))));
			//Assert.AreEqual(1,sentencia.Tablas(QueTablas.AlFrom).Count);
			sentencia.Where(p.cNombrePieza.Distinto("P0"));
			Esperado+="\n AND p.[nombrepieza]<>'P0'";
			Assert.AreEqual(Esperado+";\n",new Ejecutador(dba).Dump(sentencia));
			Assert.AreEqual(Esperado+";\n",new Ejecutador(dba).Dump(sentencia));
			Empresas contexto=new Empresas();
			contexto.cEmpresa.AsignarValor(14);
			using(Ejecutador ej=new Ejecutador(dba,contexto)){
				Esperado+="\n AND p.[empresa]=14";
				Assert.AreEqual(Esperado+";\n",ej.Dump(sentencia));
			}
		}	
		[Test]
		public void SentenciaCompuesta(){
			Piezas p=new Piezas();
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
				PartesPiezas pp=new PartesPiezas(); 
				pp.UsarFk();
				Piezas pi=pp.fkPiezas;
				Assert.AreEqual(pp,pi.TablaRelacionada);
				Sentencia s2=
					new SentenciaSelect(pi.cPieza,pi.cNombrePieza,cCantidadPartes.EsSuma(pp.cCantidad));
				Assert.AreEqual("SELECT p.[pieza], p.[nombrepieza],\n SUM(pp.[cantidad]) AS [cantidadpartes]\n FROM [piezas] p, [partespiezas] pp\n WHERE p.[empresa]=pp.[empresa]\n AND p.[pieza]=pp.[pieza]\n AND p.[empresa]=13\n AND pp.[empresa]=13\n GROUP BY p.[pieza], p.[nombrepieza];\n"
				                ,ej.Dump(s2));
				Assert.AreEqual("SELECT SUM(pp.[cantidad]) AS [cantidadpartes]\n FROM [partespiezas] pp, [piezas] p\n WHERE p.[pieza]<>p.[nombrepieza]\n AND p.[empresa]=pp.[empresa]\n AND p.[pieza]=pp.[pieza]\n AND pp.[empresa]=13\n AND p.[empresa]=13;\n"
				                ,ej.Dump(new SentenciaSelect(cCantidadPartes.EsSuma(pp.cCantidad)).Where(pi.cPieza.Distinto(pi.cNombrePieza))));
				Sentencia su=
					new SentenciaUpdate(pp,pp.cNombreParte.Es(pi.cNombrePieza.Concatenado(pp.cParte.NumeroACadena()))).Where(pp.cNombreParte.EsNulo());
				Console.WriteLine(ej.Dump(su));
				Assert.AreEqual("UPDATE [partespiezas] pp INNER JOIN [piezas] p ON pp.[empresa]=p.[empresa] AND pp.[pieza]=p.[pieza]\n " +
				                "SET pp.[nombreparte]=p.[nombrepieza] & STR(pp.[parte])\n WHERE pp.[nombreparte] IS NULL\n AND pp.[empresa]=13\n AND p.[empresa]=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE [partespiezas] pp INNER JOIN [piezas] p ON pp.[empresa]=p.[empresa] AND pp.[pieza]=p.[pieza]\n " +
				                "SET pp.[nombreparte]=p.[nombrepieza] & STR(pp.[parte])\n WHERE pp.[nombreparte] IS NULL\n AND pp.[empresa]=13\n AND p.[empresa]=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE partespiezas pp\n " +
				                "SET nombreparte=p.nombrepieza||pp.parte\n " +
				                "FROM piezas p\n " +
				                "WHERE pp.nombreparte IS NULL\n " +
				                "AND p.empresa=pp.empresa\n AND p.pieza=pp.pieza\n " +
				                "AND pp.empresa=13\n AND p.empresa=13;\n",
				                ejp.Dump(su));
				dba.TipoStuffActual=BaseDatos.TipoStuff.Inteligente;
				su=new SentenciaUpdate(pi,pi.cEstado.Es(0)).Where(pi.cEstado.EsNulo());
				Assert.AreEqual("UPDATE piezas p\n SET p.estado=0\n WHERE p.estado IS NULL\n AND p.empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE piezas p\n SET estado=0\n WHERE p.estado IS NULL\n AND p.empresa=13;\n",
				                ejp.Dump(su));
				pi.Alias="pi";
				su=new SentenciaUpdate(pp,pp.cCantidad.Es(pp.cCantidad.Mas(1))).Where(pp.cCantidad.EsNulo());
				Assert.AreEqual("UPDATE partespiezas pp\n SET pp.cantidad=pp.cantidad+1\n WHERE pp.cantidad IS NULL\n AND pp.empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE partespiezas pp\n SET cantidad=pp.cantidad+1\n WHERE pp.cantidad IS NULL\n AND pp.empresa=13;\n",
				                ejp.Dump(su));
				su=new SentenciaSelect(p.cPieza,p.cNombrePieza,pi.cNombrePieza).Where(p.cPieza.Igual(pi.cPieza.Concatenado<string>("2")));
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pi.nombrepieza\n" +
				                " FROM piezas p, piezas pi\n" +
				                " WHERE p.pieza=pi.pieza & '2'\n AND p.empresa=13\n AND pi.empresa=13;\n",
				                ej.Dump(su));
				CampoDestino<string> d=new CampoDestino<string>("otro_nombre");
				CampoDestino<int> d2=new CampoDestino<int>("otro_estado");
				su=new SentenciaSelect(p.cPieza,p.cNombrePieza,pi.cNombrePieza,d.Es(pi.cNombrePieza.Concatenado(p.cPieza)))
					.Where(p.cPieza.Igual(pi.cPieza.Concatenado("2")),d.Distinto("A"),d2.Es(pi.cEstado).Distinto(3));
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pi.nombrepieza,\n pi.nombrepieza & p.pieza AS [otro_nombre]\n" +
				                " FROM piezas p, piezas pi\n" +
				                " WHERE p.pieza=pi.pieza & '2'\n AND pi.nombrepieza & p.pieza<>'A'\n AND pi.estado<>3\n AND p.empresa=13\n AND pi.empresa=13;\n",
				                ej.Dump(su));
				NovedadesPiezas np=new NovedadesPiezas();
				np.EsFkDe(pi,pi.cPieza);
				pi.LiberadaDelContextoDelEjecutador=true;
				Assert.AreEqual(pi,np.TablaRelacionada);
				pi.Alias="p";
				su=new SentenciaUpdate(pi,pi.cEstado.Es(np.cNuevoEstado.PeroSiEsNulo(pi.cEstado)),pi.cNombrePieza.Es(pi.cNombrePieza.Concatenado(np.cNuevoEstado))).Where(pi.cPieza.Distinto("P_este"));
				Assert.AreEqual("UPDATE piezas p INNER JOIN novedadespiezas np ON p.empresa=np.empresa AND p.pieza=np.piezaauxiliar\n" +
				                " SET p.estado=NZ(np.nuevoestado, p.estado),\n p.nombrepieza=p.nombrepieza & np.nuevoestado\n" +
				                " WHERE p.pieza<>'P_este'\n AND p.empresa=13\n AND np.empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE piezas p\n " +
				                "SET estado=COALESCE(np.nuevoestado, p.estado),\n nombrepieza=p.nombrepieza||np.nuevoestado\n FROM novedadespiezas np\n" +
				                " WHERE p.pieza<>'P_este'\n AND np.empresa=p.empresa\n AND np.piezaauxiliar=p.pieza\n AND p.empresa=13\n AND np.empresa=13;\n",
				                ejp.Dump(su));
				su=new SentenciaUpdate(pi,pi.cEstado.Es(np.cNuevoEstado),pi.cCosto.SeaNulo()).Where(pi.cPieza.Distinto("P_este"));
				Assert.AreEqual("UPDATE piezas p INNER JOIN novedadespiezas np ON p.empresa=np.empresa AND p.pieza=np.piezaauxiliar\n SET p.estado=np.nuevoestado, p.costo=NULL\n WHERE p.pieza<>'P_este'\n AND p.empresa=13\n AND np.empresa=13;\n",
				                ej.Dump(su));
				
				/* Falta programar
				Assert.AreEqual("UPDATE piezas SET estado=(SELECT n.nuevoestado FROM novedadespiezas n WHERE n.empresa=empresa AND n.piezaauxiliar=pieza),\n p.costo=null\n WHERE p.pieza<>'P_este'\n AND p.empresa=13\n;\n",
				                ejp.Dump(su));
				*/
			}
		}
		[Test]
		public void MultiFk(){
			BaseDatos dba=BdAccess.SinAbrir();
			BaseDatos dbp=PostgreSql.SinAbrir();
			Piezas p=new Piezas();
			Numeros num=new Numeros();
			{
				PartesPiezas pp=new PartesPiezas();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero));
				Sentencia s=
					new SentenciaSelect(p.cPieza,p.cNombrePieza,num.cNumero,pp.cNombreParte).Where(num.cNumero.MenorOIgual(3));
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, numeros.numero, pp.nombreparte\n" +
				                " FROM piezas p, numeros, partespiezas pp\n" +
				                " WHERE numeros.numero<=3\n AND pp.empresa=p.empresa\n AND pp.pieza=p.pieza\n AND pp.parte=numeros.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesPiezas pp=new PartesPiezas();
				pp.EsFkDe(p,pp.cParte.Es(7));
				Sentencia s=
					new SentenciaSelect(p.cPieza,p.cNombrePieza,pp.cNombreParte);
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pp.nombreparte\n" +
				                " FROM piezas p, partespiezas pp\n" +
				                " WHERE pp.empresa=p.empresa\n AND pp.pieza=p.pieza\n AND pp.parte=7;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesPiezas pp=new PartesPiezas();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero),pp.cEmpresa.Es(1));
				Sentencia s=
					new SentenciaSelect(p.cPieza,p.cNombrePieza,num.cNumero,pp.cNombreParte);
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, numeros.numero, pp.nombreparte\n" +
				                " FROM piezas p, numeros, partespiezas pp\n" +
				                " WHERE pp.empresa=1\n AND pp.pieza=p.pieza\n AND pp.parte=numeros.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesPiezas pp=new PartesPiezas();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero),pp.cEmpresa.Es(1));
				Sentencia s=
					new SentenciaSelect(p.cPieza,p.cNombrePieza,pp.cNombreParte);
				Assert.Ignore("Falta considerar tablas que sirvan para los joins y no estén en los campos del select");
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pp.nombreparte\n" +
				                " FROM piezas p, numeros, partespiezas pp\n" +
				                " WHERE pp.empresa=1\n AND pp.pieza=p.pieza\n AND pp.parte=numeros.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
		}
		[Test]
		public void UpdateSuma(){
			BaseDatos dba=BdAccess.SinAbrir();
			BaseDatos dbp=PostgreSql.SinAbrir();
			PartesPiezas pp=new PartesPiezas();
			pp.UsarFk();
			Piezas pr=pp.fkPiezas;
			Assert.AreEqual(pr.TablaRelacionada,pp);
			SentenciaUpdate su=
				new SentenciaUpdate(pr,pr.cCosto.Es(pr.SelectSuma(pp.cCantidad)));
			Assert.AreEqual("UPDATE piezas p\n SET p.costo=DSUM('cantidad','partespiezas','empresa=' & p.empresa & ' AND pieza=''' & p.pieza & '''');\n"
			                ,new Ejecutador(dba).Dump(su));
			Assert.AreEqual("UPDATE piezas p\n SET costo=(SELECT SUM(pp.cantidad) FROM partespiezas pp WHERE pp.empresa=p.empresa AND pp.pieza=p.pieza);\n"
			                ,new Ejecutador(dbp).Dump(su));
			NovedadesPiezas np=new NovedadesPiezas();
			np.UsarFk();
			pr.EsFkDe(np,np.cPiezaAuxiliar);
			su=new SentenciaUpdate(pr,pr.cCosto.Es(pr.SelectSuma(np.cNuevoEstado)));
			Assert.AreEqual("UPDATE piezas p\n SET p.costo=DSUM('nuevoestado','novedadespiezas','empresa=' & p.empresa & ' AND piezaauxiliar=''' & p.pieza & '''');\n"
			                ,new Ejecutador(dba).Dump(su));
			Assert.AreEqual("UPDATE piezas p\n SET costo=(SELECT SUM(np.nuevoestado) FROM novedadespiezas np WHERE np.empresa=p.empresa AND np.piezaauxiliar=p.pieza);\n"
			                ,new Ejecutador(dbp).Dump(su));
			// Assert.Ignore("Error en el promedio geom'etrico" );
			su=new SentenciaUpdate(pr,pr.cCosto.Es(pr.SelectPromedioGeometrico(np.cNuevoEstado)));
			Assert.AreEqual("UPDATE piezas p\n " +
			                "SET p.costo=IIF(DAVG('LOG(nuevoestado)','novedadespiezas','nuevoestado>0 AND empresa=' & p.empresa & ' AND piezaauxiliar=''' & p.pieza & '''') IS NULL,NULL," +
			                "EXP(DAVG('LOG(nuevoestado)','novedadespiezas','nuevoestado>0 AND empresa=' & p.empresa & ' AND piezaauxiliar=''' & p.pieza & '''')));\n"
			                ,new Ejecutador(dba).Dump(su));
			Assert.AreEqual("UPDATE piezas p\n SET costo=(SELECT EXP(AVG(LN(np.nuevoestado))) FROM novedadespiezas np WHERE np.nuevoestado>0 AND np.empresa=p.empresa AND np.piezaauxiliar=p.pieza);\n"
			                ,new Ejecutador(dbp).Dump(su));
			pr=new Piezas();
			pp=new PartesPiezas();
			pp.EsFkDe(pr,pp.cParte.Es(1));
			su=new SentenciaUpdate(pp,pp.cCantidad.Es(pp.SelectSuma(pr.cEstado)));
			Assert.AreEqual("UPDATE partespiezas pp\n "+
			                "SET pp.cantidad=DSUM('estado','piezas','empresa=' & pp.empresa & ' AND pieza=''' & pp.pieza & '''');\n"
			                ,new Ejecutador(dba).Dump(su));
		}
		[Test]
		public void FkConDatos(){
			BaseDatos db=ProbarBdAccess.AbrirBase(2);
			Repositorio.CrearTablas(db,this.GetType().Namespace);
			Empresas e=new Empresas();
			Piezas p=new Piezas();
			PartesPiezas pp=new PartesPiezas();
			/*
			db.ExecuteNonQuery(e.SentenciaCreateTable(db));
			db.ExecuteNonQuery(p.SentenciaCreateTable(db));
			db.ExecuteNonQuery(pp.SentenciaCreateTable(db));
			*/
			Ejecutador ej=new Ejecutador(db);
			// ej.ExecuteNonQuery(new Empresas().SentenciaCreateTable(db));
			// ej.ExecuteNonQuery(new Piezas().SentenciaCreateTable(db));
			// ej.ExecuteNonQuery(new PartesPiezas().SentenciaCreateTable(db));
			if(!e.Buscar(db,7)){
				e.InsertarValores(db,e.cEmpresa.Es(7),e.cNombreEmpresa.Es("Siete"));
			}
			Empresas e2=new Empresas();
			e2.Leer(db,7);
			e.Leer(db,7);
			Assert.AreEqual("Siete",e2.cNombreEmpresa.Valor);
			p.InsertarValores(db,e,p.cPieza.Es("P11"),p.cNombrePieza.Es("El once"),p.cEstado.Es(1));
			p.InsertarValores(db,e,p.cPieza.Es("P12"),p.cNombrePieza.Es("El doce"),p.cEstado.Es(1));
			p.Leer(db,e,"P11");
			Assert.AreEqual("P11",p.cPieza.Valor);
			pp.InsertarValores(db,p,pp.cParte.Es(1),pp.cNombreParte.Es("parte 11, 1"));
			Assert.AreEqual("P11",p.cPieza.Valor);
			PartesPiezas pp2=new PartesPiezas();
			Assert.AreEqual("P11",p.cPieza.Valor);
			pp2.Leer(db,p,1);
			Assert.AreEqual("parte 11, 1",pp2.cNombreParte.Valor);
			PartesPiezas pp3=new PartesPiezas();
			pp3.Leer(db,7,"P11",1);
			pp3.UsarFk();
			Assert.AreEqual("P11",pp3.fkPiezas.cPieza.Valor);
			db.Close();
		}
		[Test]
		public void G_Enumerados(){
			BaseDatos db=ProbarBdAccess.AbrirBase(3);
			Repositorio.CrearTablas(db,this.GetType().Namespace);
			ColoresCompuestos cp=new ColoresCompuestos();
			cp.InsertarDirecto(db,1,ColoresCompuestos.ColorPrimario.Azul);
			cp.InsertarValores(db,cp.cColorCompuesto.Es(2),cp.cColorBase.Es(ColoresCompuestos.ColorPrimario.Verde));
			cp.InsertarDirecto(db,3,ColoresCompuestos.ColorPrimario.Rojo);
			cp.Leer(db,1);
			Assert.AreEqual(ColoresCompuestos.ColorPrimario.Azul,cp.cColorBase.Valor);
			db.Close();
		}
		[Test]
		public void Subselect(){
			BaseDatos dba=BdAccess.SinAbrir();
			Piezas pss=new Piezas();
			PartesPiezas pp=new PartesPiezas();
			pp.UsarFk();
			Empresas e=pp.fkEmpresas;
			pss.SubSelect("pss",pp.cEmpresa,pp.cPieza,pss.cNombrePieza.Es(pp.cNombreParte))
				.Where(pp.cParteAnterior.EsNulo(),e.cNombreEmpresa.Distinto("este"));
			pss.Alias="x";
			Piezas p=new Piezas();
			Assert.AreEqual(
				"INSERT INTO piezas (empresa, pieza, nombrepieza)\n"+
				" SELECT x.empresa, x.pieza, x.nombrepieza\n" +
				" FROM (SELECT pp.empresa, pp.pieza, pp.nombreparte AS nombrepieza\n" +
				" FROM partespiezas pp, empresas e\n" +
				" WHERE pp.parteanterior IS NULL\n AND e.nombreempresa<>'este'\n AND e.empresa=pp.empresa) x\n" +
				" WHERE x.empresa<>0;\n",
				new Ejecutador(dba).Dump(
					new SentenciaInsert(p)
					.Select(pss.cEmpresa,pss.cPieza, pss.cNombrePieza)
					.Where(pss.cEmpresa.Distinto(0))
				)
			);
			Assert.Ignore("Falta ver que funcione bien en un contexto");
		}
		[Test]
		public void SubselectGroupBy(){
			BaseDatos dba=BdAccess.SinAbrir();
			Piezas pss=new Piezas();
			PartesPiezas pp=new PartesPiezas();
			pss.SubSelect("p",pp.cEmpresa,pp.cPieza,pss.cNombrePieza.Es(pp.cNombreParte))
				.Where(pp.cParteAnterior.EsNulo())
				.GroupBy();
			Piezas p=new Piezas();
			Sentencia s=
				new SentenciaInsert(p)
				.Select(pss.cEmpresa,pss.cPieza, pss.cNombrePieza)
				.Where(pss.cEmpresa.Distinto(0));
			Assert.AreEqual(
				"INSERT INTO piezas (empresa, pieza, nombrepieza)\n"+
				" SELECT p.empresa, p.pieza, p.nombrepieza\n" +
				" FROM (SELECT pp.empresa, pp.pieza, pp.nombreparte AS nombrepieza\n" +
				" FROM partespiezas pp\n" +
				" WHERE pp.parteanterior IS NULL\n" +
				" GROUP BY pp.empresa, pp.pieza, pp.nombreparte) p\n" +
				" WHERE p.empresa<>0;\n",
				new Ejecutador(dba).Dump(s)
			);
			pp.UsarFk();
			Empresas e=pp.fkEmpresas;
			Assert.AreEqual(
				"INSERT INTO piezas (empresa, pieza, nombrepieza)\n"+
				" SELECT pp.empresa, pp.pieza, pp.nombreparte AS nombrepieza\n" +
				" FROM partespiezas pp\n" +
				" WHERE NOT EXISTS (SELECT e.empresa\n FROM empresas e\n WHERE e.empresa=pp.empresa)\n" +
				" AND pp.empresa<>0;\n",
				new Ejecutador(dba).Dump(
					new SentenciaInsert(p)
					.Select(pp.cEmpresa,pp.cPieza, p.cNombrePieza.Es(pp.cNombreParte))
					.Where(e.NoExistePara(pp),pp.cEmpresa.Distinto(0))
				)
			);
		}
		[Test]
		public void SubselectGroupBySqlite(){
			BaseDatos db=SqLite.SinAbrir();
			Piezas pss=new Piezas();
			PartesPiezas pp=new PartesPiezas();
			pss.SubSelect("p",pp.cEmpresa,pp.cPieza,pss.cNombrePieza.Es(pp.cNombreParte))
				.Where(pp.cParteAnterior.EsNulo())
				.GroupBy();
			Piezas p=new Piezas();
			Assert.AreEqual(
				"INSERT INTO piezas (empresa, pieza, nombrepieza)\n"+
				" SELECT p.empresa, p.pieza, p.nombrepieza\n" +
				" FROM (SELECT pp.empresa AS empresa, pp.pieza AS pieza,\n pp.nombreparte AS nombrepieza\n" +
				" FROM partespiezas pp\n" +
				" WHERE pp.parteanterior IS NULL\n" +
				" GROUP BY pp.empresa, pp.pieza, pp.nombreparte) p\n" +
				" WHERE p.empresa<>0;\n",
				new Ejecutador(db).Dump(
					new SentenciaInsert(p)
					.Select(pss.cEmpresa,pss.cPieza, pss.cNombrePieza)
					.Where(pss.cEmpresa.Distinto(0))
				)
			);
		}
	}
}
