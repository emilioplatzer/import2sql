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
			Assert.AreEqual("create table periodos(ano integer not null,mes integer not null,anoant integer,mesant integer,primary key(ano,mes));"
			                ,Cadena.Simplificar(p.SentenciaCreateTable(dba)));
			Piezas pr=new Piezas();
			Assert.AreEqual("create table piezas(empresa integer not null,pieza varchar(4),nombrepieza varchar(250),estado integer not null,costo double precision,primary key(empresa,pieza),foreign key(empresa)references empresas(empresa));"
			                ,Cadena.Simplificar(pr.SentenciaCreateTable(dba)));
			PartesPiezas pa=new PartesPiezas();
			Assert.AreEqual(false,dba.SoportaFkMixta);
			pa.UsarFk();
			Assert.AreEqual("partespiezas",pa.TablasFk[1].NombreTabla);
			Assert.AreEqual(Fk.Tipo.Mixta,pa.TablasFk[1].TipoFk);
			Assert.AreEqual("create table partespiezas(empresa integer not null,pieza varchar(4),parte integer not null,nombreparte varchar(250),cantidad integer,parteanterior integer,primary key(empresa,pieza,parte),foreign key(empresa,pieza)references piezas(empresa,pieza));"
			                ,Cadena.Simplificar(pa.SentenciaCreateTable(dba)));
			Assert.AreEqual("create table partespiezas(empresa integer not null,pieza varchar(4),parte integer not null,nombreparte varchar(250),cantidad integer,parteanterior integer,primary key(empresa,pieza,parte),foreign key(empresa,pieza)references piezas(empresa,pieza),foreign key(empresa,pieza,parteanterior)references partespiezas(empresa,pieza,parte));"
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
		[Test]
		public void SentenciaUpdate(){
			Piezas p=new Piezas();
			BaseDatos dba=BdAccess.SinAbrir();
			dba.TipoStuffActual=BaseDatos.TipoStuff.Siempre;
			Assert.AreEqual("UPDATE [piezas] SET [pieza]='P1',\n [nombrepieza]='Pieza 1';\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cPieza.Set("P1"),p.cNombrePieza.Set("Pieza 1"))));
			string Esperado="UPDATE [piezas] SET [pieza]='P1',\n [nombrepieza]='Pieza 1'\n WHERE [pieza]='P3'\n AND ([nombrepieza] IS NULL OR [nombrepieza]<>[pieza])";
			Assert.AreEqual(Esperado+";\n",
			                new Ejecutador(dba)
			                .Dump(new SentenciaUpdate(p,p.cPieza.Set("P1"),p.cNombrePieza.Set("Pieza 1"))
			                      .Where(p.cPieza.Igual("P3")
			                             .And(p.cNombrePieza.EsNulo()
			                                  .Or(p.cNombrePieza.Distinto(p.cPieza))))));
			SentenciaUpdate sentencia=new SentenciaUpdate(p,p.cPieza.Set("P1"),p.cNombrePieza.Set("Pieza 1"));
			sentencia.Where(p.cPieza.Igual("P3")
			                .And(p.cNombrePieza.EsNulo()
			                     .Or(p.cNombrePieza.Distinto(p.cPieza))));
			Assert.AreEqual(1,sentencia.Tablas().Count);
			//Assert.AreEqual("piezas",(sentencia.Tablas().Keys)[0].NombreTabla);
			sentencia.Where(p.cNombrePieza.Distinto("P0"));
			Esperado+="\n AND [nombrepieza]<>'P0'";
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
				Assert.AreEqual("SELECT p.[pieza], p.[nombrepieza], SUM(pa.[cantidad]) AS [cantidadpartes]\n FROM [piezas] p, [partespiezas] pa\n WHERE p.[empresa]=pa.[empresa]\n AND p.[pieza]=pa.[pieza]\n AND p.[empresa]=13\n AND pa.[empresa]=13\n GROUP BY p.[pieza], p.[nombrepieza];\n"
				                ,ej.Dump(s2));
				Assert.AreEqual("SELECT SUM(p.[cantidad]) AS [cantidadpartes]\n FROM [partespiezas] p, [piezas] pi\n WHERE pi.[pieza]<>pi.[nombrepieza]\n AND pi.[empresa]=p.[empresa]\n AND pi.[pieza]=p.[pieza]\n AND p.[empresa]=13\n AND pi.[empresa]=13;\n"
				                ,ej.Dump(new SentenciaSelect(cCantidadPartes.EsSuma(pp.cCantidad)).Where(pi.cPieza.Distinto(pi.cNombrePieza))));
				Sentencia su=
					new SentenciaUpdate(pp,pp.cNombreParte.Set(pi.cNombrePieza.Concatenado(pp.cParte))).Where(pp.cNombreParte.EsNulo());
				Assert.AreEqual("UPDATE [partespiezas] p INNER JOIN [piezas] pi ON p.[empresa]=pi.[empresa] AND p.[pieza]=pi.[pieza]\n " +
				                "SET p.[nombreparte]=(pi.[nombrepieza] & p.[parte])\n WHERE p.[nombreparte] IS NULL\n AND p.[empresa]=13\n AND pi.[empresa]=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE [partespiezas] p INNER JOIN [piezas] pi ON p.[empresa]=pi.[empresa] AND p.[pieza]=pi.[pieza]\n " +
				                "SET p.[nombreparte]=(pi.[nombrepieza] & p.[parte])\n WHERE p.[nombreparte] IS NULL\n AND p.[empresa]=13\n AND pi.[empresa]=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE partespiezas " +
				                "SET nombreparte=(SELECT (pi.nombrepieza||parte) FROM piezas pi WHERE pi.empresa=empresa AND pi.pieza=pieza)\n WHERE nombreparte IS NULL\n AND empresa=13;\n",
				                ejp.Dump(su));
				dba.TipoStuffActual=BaseDatos.TipoStuff.Inteligente;
				su=new SentenciaUpdate(pi,pi.cEstado.Set(0)).Where(pi.cEstado.EsNulo());
				Assert.AreEqual("UPDATE piezas SET estado=0\n WHERE estado IS NULL\n AND empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE piezas SET estado=0\n WHERE estado IS NULL\n AND empresa=13;\n",
				                ejp.Dump(su));
				su=new SentenciaUpdate(pp,pp.cCantidad.Set(pp.cCantidad.Mas(1))).Where(pp.cCantidad.EsNulo());
				Assert.AreEqual("UPDATE partespiezas SET cantidad=cantidad+1\n WHERE cantidad IS NULL\n AND empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE partespiezas SET cantidad=cantidad+1\n WHERE cantidad IS NULL\n AND empresa=13;\n",
				                ejp.Dump(su));
				su=new SentenciaSelect(p.cPieza,p.cNombrePieza,pi.cNombrePieza).Where(p.cPieza.Igual(pi.cPieza.Concatenado("2")));
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pi.nombrepieza\n" +
				                " FROM piezas p, piezas pi\n" +
				                " WHERE p.pieza=(pi.pieza & '2')\n AND p.empresa=13\n AND pi.empresa=13;\n",
				                ej.Dump(su));
				CampoDestino<string> d=new CampoDestino<string>("otro_nombre");
				CampoDestino<int> d2=new CampoDestino<int>("otro_estado");
				su=new SentenciaSelect(p.cPieza,p.cNombrePieza,pi.cNombrePieza,d.Es(pi.cNombrePieza.Concatenado(p.cPieza)))
					.Where(p.cPieza.Igual(pi.cPieza.Concatenado("2")),d.Distinto("A"),d2.Es(pi.cEstado).Distinto(3));
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pi.nombrepieza, (pi.nombrepieza & p.pieza) AS [otro_nombre]\n" +
				                " FROM piezas p, piezas pi\n" +
				                " WHERE p.pieza=(pi.pieza & '2')\n AND (pi.nombrepieza & p.pieza)<>'A'\n AND pi.estado<>3\n AND p.empresa=13\n AND pi.empresa=13;\n",
				                ej.Dump(su));
				NovedadesPiezas np=new NovedadesPiezas();
				np.EsFkDe(pi,pi.cPieza);
				pi.LiberadaDelContextoDelEjecutador=true;
				Assert.AreEqual(pi,np.TablaRelacionada);
				su=new SentenciaUpdate(pi,pi.cEstado.Set(np.cNuevoEstado),pi.cNombrePieza.Set(pi.cNombrePieza.Concatenado(np.cNuevoEstado))).Where(pi.cPieza.Distinto("P_este"));
				Assert.AreEqual("UPDATE piezas p INNER JOIN novedadespiezas n ON p.empresa=n.empresa AND p.pieza=n.piezaauxiliar\n" +
				                " SET p.estado=n.nuevoestado,\n p.nombrepieza=(p.nombrepieza & n.nuevoestado)\n" +
				                " WHERE p.pieza<>'P_este'\n AND n.empresa=13;\n",
				                ej.Dump(su));
				Assert.AreEqual("UPDATE piezas " +
				                "SET estado=(SELECT n.nuevoestado FROM novedadespiezas n WHERE n.empresa=empresa AND n.piezaauxiliar=pieza),\n" +
				                " nombrepieza=(SELECT (nombrepieza||n.nuevoestado) FROM novedadespiezas n WHERE n.empresa=empresa AND n.piezaauxiliar=pieza)\n" +
				                " WHERE pieza<>'P_este';\n",
				                ejp.Dump(su));
				su=new SentenciaUpdate(pi,pi.cEstado.Set(np.cNuevoEstado),pi.cCosto.SetNull()).Where(pi.cPieza.Distinto("P_este"));
				Assert.AreEqual("UPDATE piezas p INNER JOIN novedadespiezas n ON p.empresa=n.empresa AND p.pieza=n.piezaauxiliar\n SET p.estado=n.nuevoestado,\n p.costo=null\n WHERE p.pieza<>'P_este'\n AND n.empresa=13;\n",
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
					new SentenciaSelect(p.cPieza,p.cNombrePieza,num.cNumero,pp.cNombreParte).Where(num.cNumero.Operado("<=",3));
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, n.numero, pa.nombreparte\n" +
				                " FROM piezas p, numeros n, partespiezas pa\n" +
				                " WHERE n.numero<=3\n AND pa.empresa=p.empresa\n AND pa.pieza=p.pieza\n AND pa.parte=n.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesPiezas pp=new PartesPiezas();
				pp.EsFkDe(p,pp.cParte.Es(7));
				Sentencia s=
					new SentenciaSelect(p.cPieza,p.cNombrePieza,pp.cNombreParte);
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pa.nombreparte\n" +
				                " FROM piezas p, partespiezas pa\n" +
				                " WHERE pa.empresa=p.empresa\n AND pa.pieza=p.pieza\n AND pa.parte=7;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesPiezas pp=new PartesPiezas();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero),pp.cEmpresa.Es(1));
				Sentencia s=
					new SentenciaSelect(p.cPieza,p.cNombrePieza,num.cNumero,pp.cNombreParte);
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, n.numero, pa.nombreparte\n" +
				                " FROM piezas p, numeros n, partespiezas pa\n" +
				                " WHERE pa.empresa=1\n AND pa.pieza=p.pieza\n AND pa.parte=n.numero;\n",
				                new Ejecutador(dba).Dump(s));
			}
			{
				PartesPiezas pp=new PartesPiezas();
				pp.EsFkDe(p,pp.cParte.Es(num.cNumero),pp.cEmpresa.Es(1));
				Sentencia s=
					new SentenciaSelect(p.cPieza,p.cNombrePieza,pp.cNombreParte);
				Assert.Ignore("Falta considerar tablas que sirvan para los joins y no estén en los campos del select");
				Assert.AreEqual("SELECT p.pieza, p.nombrepieza, pa.nombreparte\n" +
				                " FROM piezas p, numeros n, partespiezas pa\n" +
				                " WHERE pa.empresa=1\n AND pa.pieza=p.pieza\n AND pa.parte=n.numero;\n",
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
				new SentenciaUpdate(pr,pr.cCosto.Set(pr.SelectSuma(pp.cCantidad)));
			Assert.AreEqual("UPDATE piezas SET costo=DSum('cantidad','partespiezas','empresa=''' & empresa & ''' AND pieza=''' & pieza & '''');\n"
			                ,new Ejecutador(dba).Dump(su));
			Assert.AreEqual("UPDATE piezas SET costo=(SELECT SUM(zz.cantidad) FROM partespiezas zz WHERE zz.empresa=empresa AND zz.pieza=pieza);\n"
			                ,new Ejecutador(dbp).Dump(su));
			NovedadesPiezas np=new NovedadesPiezas();
			np.UsarFk();
			pr.EsFkDe(np,np.cPiezaAuxiliar);
			su=new SentenciaUpdate(pr,pr.cCosto.Set(pr.SelectSuma(np.cNuevoEstado)));
			Assert.AreEqual("UPDATE piezas SET costo=DSum('nuevoestado','novedadespiezas','empresa=''' & empresa & ''' AND piezaauxiliar=''' & pieza & '''');\n"
			                ,new Ejecutador(dba).Dump(su));
			Assert.AreEqual("UPDATE piezas SET costo=(SELECT SUM(zz.nuevoestado) FROM novedadespiezas zz WHERE zz.empresa=empresa AND zz.piezaauxiliar=pieza);\n"
			                ,new Ejecutador(dbp).Dump(su));
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
			pss.SubSelect(pp.cEmpresa,pp.cPieza,pss.cNombrePieza.Es(pp.cNombreParte))
				.Where(pp.cParteAnterior.EsNulo());
			Piezas p=new Piezas();
			Assert.AreEqual(
				"INSERT INTO piezas (empresa, pieza, nombrepieza)"+
				" SELECT p.empresa, p.pieza, p.nombrepieza\n" +
				" FROM (SELECT p.empresa, p.pieza, p.nombreparte AS nombrepieza\n" +
				" FROM partespiezas p\n" +
				" WHERE p.parteanterior IS NULL) p\n" +
				" WHERE p.empresa<>0;\n",
				new Ejecutador(dba).Dump(
					new SentenciaInsert(p)
					.Select(pss.cEmpresa,pss.cPieza, pss.cNombrePieza)
					.Where(pss.cEmpresa.Distinto(0))
				)
			);
		}
		[Test]
		public void SubselectGroupBy(){
			BaseDatos dba=BdAccess.SinAbrir();
			Piezas pss=new Piezas();
			PartesPiezas pp=new PartesPiezas();
			pss.SubSelect(pp.cEmpresa,pp.cPieza,pss.cNombrePieza.Es(pp.cNombreParte))
				.Where(pp.cParteAnterior.EsNulo())
				.GroupBy();
			Piezas p=new Piezas();
			Assert.AreEqual(
				"INSERT INTO piezas (empresa, pieza, nombrepieza)"+
				" SELECT p.empresa, p.pieza, p.nombrepieza\n" +
				" FROM (SELECT p.empresa, p.pieza, p.nombreparte AS nombrepieza\n" +
				" FROM partespiezas p\n" +
				" WHERE p.parteanterior IS NULL\n" +
				" GROUP BY p.empresa, p.pieza, p.nombreparte) p\n" +
				" WHERE p.empresa<>0;\n",
				new Ejecutador(dba).Dump(
					new SentenciaInsert(p)
					.Select(pss.cEmpresa,pss.cPieza, pss.cNombrePieza)
					.Where(pss.cEmpresa.Distinto(0))
				)
			);
		}
		[Test]
		public void SubselectGroupBySqlite(){
			BaseDatos db=SqLite.SinAbrir();
			Piezas pss=new Piezas();
			PartesPiezas pp=new PartesPiezas();
			pss.SubSelect(pp.cEmpresa,pp.cPieza,pss.cNombrePieza.Es(pp.cNombreParte))
				.Where(pp.cParteAnterior.EsNulo())
				.GroupBy();
			Piezas p=new Piezas();
			Assert.AreEqual(
				"INSERT INTO piezas (empresa, pieza, nombrepieza)"+
				" SELECT p.empresa, p.pieza, p.nombrepieza\n" +
				" FROM (SELECT p.empresa AS empresa, p.pieza AS pieza, p.nombreparte AS nombrepieza\n" +
				" FROM partespiezas p\n" +
				" WHERE p.parteanterior IS NULL\n" +
				" GROUP BY p.empresa, p.pieza, p.nombreparte) p\n" +
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
