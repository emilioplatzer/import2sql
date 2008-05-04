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
	public class Periodos:Tabla{
		[Pk] public CampoEntero cAno;
		[Pk] public CampoEntero cMes;
		public CampoEnteroOpcional cAnoAnt;
		public CampoEnteroOpcional cMesAnt;
	}
	public class Empresas:Tabla{
		[Pk] public CampoEntero cEmpresa;
		public CampoNombre cNombreEmpresa;
	}
	public class Piezas:Tabla{
		[Pk] public CampoEntero cEmpresa;
		[Pk] public CampoPieza cPieza;
		public CampoNombre cNombrePieza;
		public CampoEntero cEstado;
		public CampoRealOpcional cCosto;
		[Fk] public Empresas fkEmpresas;
	}
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
	public class NovedadesPiezas:Tabla{
		[Pk] public CampoEntero cEmpresa;
		[Pk] public CampoPieza cPiezaAuxiliar;
		public CampoEnteroOpcional cNuevoEstado;
	}
	public class Numeros:Tabla{
		[Pk] public CampoEntero cNumero;
	}
	public class ColoresCompuestos:Tabla{
		public enum ColorPrimario{Rojo,Verde,Azul};
		[Pk] public CampoEntero cColorCompuesto;
		public CampoEnumerado<ColorPrimario> cColorBase;
	}
	#pragma warning restore 649
	public class CampoPieza:CampoProducto{};
	[TestFixture]
	public class prTabla{
		public prTabla(){
		}
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
		}
		[Test]
		public void SentenciaInsert(){
			Piezas p=new Piezas();
			BaseDatos dba=BdAccess.SinAbrir();
			Assert.AreEqual("INSERT INTO piezas (pieza, nombrepieza) SELECT p.pieza, p.pieza AS nombrepieza\n FROM piezas p;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(new Piezas()).Select(p.cPieza,p.cNombrePieza.Es(p.cPieza))));
			PartesPiezas pp=new PartesPiezas();
			pp.UsarFk();
			Piezas pr=pp.fkPiezas;
			NovedadesPiezas np=new NovedadesPiezas();
			np.EsFkDe(pr,pr.cPieza);
			Assert.AreEqual("INSERT INTO partespiezas (pieza, cantidad, nombreparte) SELECT p.pieza, SUM(p.costo) AS cantidad, n.nuevoestado AS nombreparte\n FROM piezas p, novedadespiezas n\n WHERE n.empresa=p.empresa\n AND n.piezaauxiliar=p.pieza\n GROUP BY p.pieza, n.nuevoestado;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(pp).Select(pr.cPieza,pp.cCantidad.EsSuma(pr.cCosto),pp.cNombreParte.Es(np.cNuevoEstado))));
			Assert.AreEqual("INSERT INTO partespiezas (pieza, cantidad, nombreparte) " +
			                "SELECT p.pieza, SUM(p.costo) AS cantidad, n.nuevoestado AS nombreparte\n " +
			                "FROM piezas p, novedadespiezas n\n " +
			                "WHERE n.empresa=p.empresa\n AND n.piezaauxiliar=p.pieza\n " +
			                "GROUP BY p.pieza, n.nuevoestado\n " +
			                "HAVING COUNT(*)>=2\n AND COUNT(p.costo)>=1;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(pp)
				      .Select(pr.cPieza,pp.cCantidad.EsSuma(pr.cCosto),pp.cNombreParte.Es(np.cNuevoEstado))
				      .Having(new CampoDestino<int>("cantidad_registros").EsCount().MayorOIgual(2),
				              new CampoDestino<int>("suma_costos").EsCount(p.cCosto).MayorOIgual(1))
				     )
			);
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza, cantidad, nombreparte) SELECT 1 AS empresa, p.pieza, SUM(p.costo) AS cantidad, n.nuevoestado AS nombreparte\n FROM piezas p, novedadespiezas n\n WHERE n.empresa=p.empresa\n AND n.piezaauxiliar=p.pieza\n GROUP BY p.pieza, n.nuevoestado;\n",
				new Ejecutador(dba)
				.Dump(new SentenciaInsert(pp).Select(pp.cEmpresa.Es(1),pr.cPieza,pp.cCantidad.EsSuma(pr.cCosto),pp.cNombreParte.Es(np.cNuevoEstado))));
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza) VALUES (1, 'PROD1');\n",
			    new Ejecutador(dba)
			    .Dump(new SentenciaInsert(pp).Valores(pp.cEmpresa.Es(1),pr.cPieza.Es("PROD1"))));
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza, parte) SELECT p.empresa, p.pieza, 1 AS parte\n FROM piezas p;\n",
                new Ejecutador(dba)
                .Dump(new SentenciaInsert(pp).Select(p,pp.cParte.Es(1))));
			p.UsarFk();
			Empresas e=p.fkEmpresas;
			Assert.AreEqual("INSERT INTO partespiezas (empresa, pieza, parte) SELECT e.empresa, p.pieza, 1 AS parte\n FROM empresas e, piezas p\n WHERE e.empresa=p.empresa;\n",
                new Ejecutador(dba)
                .Dump(new SentenciaInsert(pp).Select(e,p,pp.cParte.Es(1))));
		}
	}
}
