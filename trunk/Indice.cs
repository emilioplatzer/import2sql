/*
 * Creado por SharpDevelop.
 * Usuario: Emilio
 * Fecha: 21/03/2008
 * Hora: 19:42
 * 
 * Para cambiar esta plantilla use Herramientas | Opciones | Codificación | Editar Encabezados Estándar
 */

using System;
using System.Data;
using System.Text;
using NUnit.Framework;
using TodoASql;
using Modelador;

namespace Indices
{
	public class CampoProducto:CampoChar{ public CampoProducto():base(4){} };
	public class CampoNombre:CampoChar{ public CampoNombre():base(250){} };
	public class CampoAgrupacion:CampoChar{ public CampoAgrupacion():base(9){} };
	public class CampoGrupo:CampoChar{ public CampoGrupo():base(9){} };
	public class CampoPonderador:CampoReal{};
	public class CampoNivel:CampoEnteroOpcional{}
	public class CampoPrecio:CampoReal{};
	public class CampoIndice:CampoReal{};
	public class CampoFactor:CampoReal{};
	public class CampoPeriodo:CampoChar{ public CampoPeriodo():base(4+2){} }
	public class EjecutadorSql:TodoASql.EjecutadorSql{
		public EjecutadorSql(BaseDatos db,Parametros[] param)
			:base(db,param)
		{
		}
		public EjecutadorSql(BaseDatos db,params object[] paramPlanos)
			:base(db,paramPlanos)
		{
		}
		/*
		protected override string AdaptarSentecia(TodoASql.SentenciaSql sentencia)
		{
			return base.AdaptarSentecia(
				sentencia.ToString()
				.Replace("grupos#filtro","grupos.agrupacion={agrupacion}")
			);
		}
		*/
	}
	public class RepositorioIndice:Repositorio
	{
		public class Productos:Tabla{
			[Pk] public CampoProducto cProducto;
			public CampoNombre	cNombreProducto;
			public double Ponderador(Grupos grupo){
				Grupos hoja=new Grupos();
				hoja.Leer(grupo.db,grupo.cAgrupacion,cProducto);
				return hoja.cPonderador.Valor/grupo.cPonderador.Valor;
			}
		}
		public class Agrupaciones:Tabla{
			[Pk] public CampoAgrupacion cAgrupacion;
			public CampoNombre cNombreAgrupacion;
		}
		public class Grupos:Tabla{
			[Pk] public CampoAgrupacion cAgrupacion;
			[Pk] public CampoGrupo cGrupo;
			public CampoNombre cNombreGrupo;
			public CampoGrupo cGrupoPadre;
			public CampoPonderador cPonderador;
			public CampoNivel cNivel;
			public CampoLogico cEsProducto;
			public ExpresionSql InPadresWhere(ExpresionSql e){
				return new ExpresionSql(
					this.cGrupoPadre,
					new LiteralSql(" IN (SELECT "),
					this.cGrupo,
					new LiteralSql(" FROM grupos WHERE "),
					e,
					new LiteralSql(")"));
			}
		}
		public class Numeros:Tabla{
			[Pk] public CampoEntero cNumero;
		}
		public class AuxGrupos:Tabla{
			[Pk] public CampoAgrupacion cAgrupacion;
			[Pk] public CampoGrupo cGrupo;
			public CampoPonderador cPonderadorOriginal;
			public CampoPonderador cSumaPonderadorHijos;	
		}
		public class Periodos:Tabla{
			[Pk] public CampoPeriodo cPeriodo;
			public CampoPeriodo cPeriodoAnterior;
			public CampoEntero cAno;
			public CampoEntero cMes;
			public Periodos(BaseDatos db,int ano, int mes){
				LeerNoPk(db,"ano",ano,"mes",mes);
			}
			public Periodos(){}
			public Periodos CrearProximo(){
				int ano=cAno.Valor;
				int mes=cMes.Valor+1;
				if(mes==13){
					mes=1; 
					ano++;
				}
				Periodos p=new Periodos();
				using(Insertador ins=p.Insertar(db)){
					p.cPeriodo[ins]=ano.ToString()+((int)mes).ToString("00");
					p.cAno[ins]=ano;
					p.cMes[ins]=mes;
					p.cPeriodoAnterior[ins]=cPeriodo;
				}
				return new Periodos(db,ano,mes);
			}
		}
		public class CalPro:Tabla{
			[Pk] public CampoPeriodo cPeriodo;
			[Pk] public CampoProducto cProducto;
			public CampoPrecio cPromedio;
		}
		public class CalGru:Tabla{
			[Pk] public CampoPeriodo cPeriodo;
			[Pk] public CampoAgrupacion cAgrupacion;
			[Pk] public CampoGrupo cGrupo;
			public CampoIndice cIndice;
			public CampoFactor cFactor;
			public CalGru(){}
			public CalGru(BaseDatos db,Periodos p,Grupos g){
				Leer(db,p.cPeriodo,g.cAgrupacion,g.cGrupo);
			}
			public CalGru(BaseDatos db,Periodos p,Agrupaciones a){
				Leer(db,p.cPeriodo,a.cAgrupacion,a.cAgrupacion);
			}
		}
		RepositorioIndice(BaseDatos db)
			:base(db)
		{
		}
		public static RepositorioIndice Crear(BaseDatos db){
			RepositorioIndice rta=new RepositorioIndice(db);
			rta.CrearTablas();
			return rta;
		}
		public override void CrearTablas(){
			base.CrearTablas();
			for(int i=0; i<=20; i++){
				db.ExecuteNonQuery("insert into numeros (numero) values ("+i.ToString()+")");
			}
		}
		public static RepositorioIndice Abrir(BaseDatos db){
			return new RepositorioIndice(db);			
		}
		public Productos AbrirProducto(string codigo){
			Productos p=new Productos();
			p.Leer(db,codigo);
			return p;
		}
		public Productos CrearProducto(string codigo){
			Productos p=new Productos();
			using(Insertador ins=p.Insertar(db)){
				p.cProducto[ins]=codigo;
			}
			return AbrirProducto(codigo);
		}
		public Grupos AbrirGrupo(string agrupacion,string codigo){
			Grupos g=new Grupos();
			g.Leer(db,agrupacion,codigo);
			return g;
		}
		public Agrupaciones AbrirAgrupacion(string agrupacion){
			Agrupaciones a=new Agrupaciones();
			a.Leer(db,agrupacion);
			return a;
		}
		public Grupos CrearGrupo(string codigo){
			return CrearGrupo(codigo,codigo,"",1);
		}
		public Grupos CrearGrupo(string agrupacion,string codigo,string codigopadre,double ponderador){
			Grupos g=new Grupos();
			using(Insertador ins=g.Insertar(db)){
				g.cGrupo[ins]=codigo;
				g.cPonderador[ins]=ponderador;
				if(codigopadre==""){
				}else{
					g.cGrupoPadre[ins]=codigopadre;
				}
				g.cAgrupacion[ins]=agrupacion;
				// ins.InsertarSiHayCampos();
			}
			return AbrirGrupo(agrupacion,codigo);
		}
		public Grupos CrearGrupo(string codigo,Grupos padre,double ponderador){
			return CrearGrupo(padre.cAgrupacion.Valor,codigo,padre.cGrupo.Valor,ponderador);
		}
		public Grupos CrearGrupo(string codigo,Agrupaciones raiz,double ponderador){
			return CrearGrupo(raiz.cAgrupacion.Valor,codigo,raiz.cAgrupacion.Valor,ponderador);
		}
		public Agrupaciones CrearAgrupacion(string codigo){
			Agrupaciones a=new Agrupaciones();
			using(Insertador ins=a.Insertar(db)){
				a.cAgrupacion[ins]=codigo;
				// ins.InsertarSiHayCampos();
			}
			CrearGrupo(codigo,codigo,null,1);
			return AbrirAgrupacion(codigo);
		}
		public void CrearHoja(Productos producto,Grupos grupo,double ponderador){
			Grupos g=new Grupos();
			using(Insertador ins=g.Insertar(db)){
				g.cAgrupacion[ins]=grupo.cAgrupacion;
				g.cGrupo[ins]=producto.cProducto;
				g.cGrupoPadre[ins]=grupo.cGrupo;
				g.cPonderador[ins]=ponderador;
				g.cEsProducto[ins]=db.Verdadero;
				// ins.InsertarSiHayCampos();
			}			
		}
		public static Periodos CrearPeriodo(BaseDatos db,int ano, int mes){
			Periodos p=new Periodos();
			using(Insertador ins=p.Insertar(db)){
				p.cPeriodo[ins]=ano.ToString()+mes.ToString("00");
				p.cAno[ins]=ano;
				p.cMes[ins]=mes;
			}
			return new Periodos(db,ano,mes);
		}
		public Periodos CrearPeriodo(int ano, int mes){
			return CrearPeriodo(db,ano,mes);
		}
		public Periodos AbrirPeriodo(int ano, int mes){
			Periodos p=new Periodos();
			p.Leer(db,"ano",ano,"mes",mes);
			return p;
		}
		public void RegistrarPromedio(Periodos per,Productos prod,double promedio){
			CalPro t=new CalPro();
			using(Insertador ins=t.Insertar(db)){
				t.cPeriodo[ins]=per.cPeriodo;
				t.cProducto[ins]=prod.cProducto;
				t.cPromedio[ins]=promedio;
				// ins.InsertarSiHayCampos();
			}			
		}
		public void CalcularPonderadores(Agrupaciones agrupacion){
			#if SuperSql
			using(Ejecutador ej=new Ejecutador(db,agrupacion)){
				Grupos grupos=new Grupos();
				ej.Ejecutar(
					new SentenciaUpdate(grupos,grupos.cNivel.Set(0),grupos.cPonderador.Set(1.0))
					.Where(grupos.cGrupoPadre.EsNulo())); // .And(grupos.cAgrupacion.Igual(grupo.cAgrupacion))));
				for(int i=0;i<10;i++){
					ej.Ejecutar(
						new SentenciaUpdate(grupos,grupos.cNivel.Set(i+1))
						.Where(grupos.InPadresWhere(grupos.cNivel.Igual(i))));
				}
			}
			#endif
			using(EjecutadorSql ej=new EjecutadorSql(db,"agrupacion",agrupacion.cAgrupacion.Valor)){
				/*
				ej.ExecuteNonQuery(@"
					UPDATE grupos SET nivel=0,ponderador=1
					  WHERE grupopadre is null 
					    AND grupos#filtro;
				");
				for(int i=0;i<10;i++){
				ej.ExecuteNonQuery(new SentenciaSql(db,@"
					UPDATE grupos SET nivel={nivel}+1
					  WHERE (grupopadre) 
						IN (SELECT grupo 
                             FROM grupos 
                             WHERE nivel={nivel} 
                               AND agrupacion={agrupacion})
                        AND agrupacion={agrupacion}
				").Arg("nivel",i));
				}
				*/
				for(int i=9;i>=0;i--){ // Subir ponderadores nulos
					if(db.GetType()==typeof(BdAccess)){
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos p SET p.ponderador=
							    DSum('ponderador','grupos','grupopadre=''' & grupo & ''' and agrupacion=''' & agrupacion & '''')
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
		                        AND ponderador IS NULL
						").Arg("nivel",i));
					}else{
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos SET ponderador=
							    (SELECT sum(h.ponderador)
							       FROM grupos h
							       WHERE h.grupopadre=grupos.grupo
							         AND h.agrupacion=grupos.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
		                        AND ponderador IS NULL
						").Arg("nivel",i));
					}
				}
				for(int i=1;i<10;i++){
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						INSERT INTO auxgrupos (agrupacion,grupo,ponderadororiginal,sumaponderadorhijos)
						  SELECT p.agrupacion,p.grupo,p.ponderador,sum(h.ponderador)
						    FROM grupos p INNER JOIN grupos h ON p.agrupacion=h.agrupacion AND p.grupo=h.grupopadre
						    WHERE h.agrupacion={agrupacion} 
	                          AND h.nivel={nivel}
						    GROUP BY p.agrupacion,p.grupo,p.ponderador;
					").Arg("nivel",i));
					if(db.GetType()==typeof(BdAccess)){
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos h INNER JOIN auxgrupos a ON a.grupo=h.grupopadre AND a.agrupacion=h.agrupacion
                              SET h.ponderador=h.ponderador*a.ponderadororiginal/a.sumaponderadorhijos
							  WHERE h.agrupacion={agrupacion} 
		                        AND h.nivel={nivel}
						").Arg("nivel",i));
					}else{
						ej.ExecuteNonQuery(new SentenciaSql(db,@"
							UPDATE grupos SET ponderador=ponderador
							    *(SELECT a.ponderadororiginal/a.sumaponderadorhijos
							       FROM auxgrupos a 
							       WHERE a.grupo=grupos.grupopadre
							         AND a.agrupacion=grupos.agrupacion)
							  WHERE agrupacion={agrupacion} 
		                        AND nivel={nivel}
						").Arg("nivel",i));
					}
				}
			}
		}
		public void CalcularMesBase(Periodos per,Agrupaciones agrupacion){
			using(EjecutadorSql ej=new EjecutadorSql(db,"periodo",per.cPeriodo.Valor,"agrupacion",agrupacion.cAgrupacion.Valor)){
				ej.ExecuteNonQuery(@"
					insert into calgru (periodo,agrupacion,grupo,indice,factor)
					  select {periodo},agrupacion,grupo,100,1
					    from grupos
					    where agrupacion={agrupacion};
				");
			}
		}
		public void CalcularCalGru(Periodos periodo,Agrupaciones agrupacion){
			using(EjecutadorSql ej=new EjecutadorSql(db,"periodo",periodo.cPeriodo.Valor,"agrupacion",agrupacion.cAgrupacion.Valor)){
				/*
				ej.ExecuteNonQuery(@"
					insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
					  select per.ano,per.mes,g.agrupacion,g.grupo,cg0.indice*cp1.promedio/cp0.promedio,cg0.factor
					    from ((((grupos as g 
							 inner join productos as p on g.grupo=p.producto)
					         inner join calgru as cg0 on g.grupo=cg0.grupo and g.agrupacion=cg0.agrupacion)
					         inner join periodos as per on per.anoant=cg0.ano and per.mesant=cg0.mes)
					         inner join calpro as cp0 on cp0.ano=per.anoant and cp0.mes=per.mesant and cp0.producto=p.producto)
					         inner join calpro as cp1 on cp1.ano=per.ano and cp1.mes=per.mes and cp1.producto=p.producto
					    where g.agrupacion={agrupacion}
					      and per.ano={ano}
					      and per.mes={mes}
				");
				*/
				ej.ExecuteNonQuery(@"
					insert into calgru (periodo,agrupacion,grupo,indice,factor)
					  select per.periodo,g.agrupacion,g.grupo,cg0.indice*cp1.promedio/cp0.promedio,cg0.factor
					    from grupos as g, 
							 productos as p,
					         calgru as cg0,
					         periodos as per, 
					         calpro as cp0, 
					         calpro as cp1
					    where g.agrupacion={agrupacion}
					      and per.periodo={periodo}
					      and g.grupo=p.producto
					      and g.grupo=cg0.grupo and g.agrupacion=cg0.agrupacion
					      and per.periodoanterior=cg0.periodo 
					      and cp0.periodo=per.periodoanterior and cp0.producto=p.producto
					      and cp1.periodo=per.periodo and cp1.producto=p.producto
				");
				for(int i=9;i>=0;i--){
					/*
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						insert into calgru (ano,mes,agrupacion,grupo,indice,factor)
						  select cg.ano,cg.mes,gp.agrupacion,gp.grupo
								,sum(cg.indice*gh.ponderador)/sum(gh.ponderador)
								,sum(cg.factor*gh.ponderador)/sum(gh.ponderador)
						    from (grupos gh
						         inner join calgru as cg on gh.grupo=cg.grupo and gh.agrupacion=cg.agrupacion)
						         inner join grupos gp on gp.agrupacion=gh.agrupacion and gp.grupo=gh.grupopadre
						    where cg.agrupacion={agrupacion}
						      and cg.ano={ano}
						      and cg.mes={mes}
						      and gh.nivel={nivel}
						    group by cg.ano,cg.mes,gp.agrupacion,gp.grupo;
					").Arg("nivel",i));
					*/
					ej.ExecuteNonQuery(new SentenciaSql(db,@"
						insert into calgru (periodo,agrupacion,grupo,indice,factor)
						  select cg.periodo,gp.agrupacion,gp.grupo
								,sum(cg.indice*gh.ponderador)/sum(gh.ponderador)
								,sum(cg.factor*gh.ponderador)/sum(gh.ponderador)
						    from grupos gh,
						         calgru as cg,
						         grupos gp 
						    where cg.agrupacion={agrupacion}
						      and cg.periodo={periodo}
						      and gh.nivel={nivel}
						      and gh.grupo=cg.grupo and gh.agrupacion=cg.agrupacion
						      and gp.agrupacion=gh.agrupacion and gp.grupo=gh.grupopadre
						    group by cg.periodo,gp.agrupacion,gp.grupo;
					").Arg("nivel",i));
				}
			}
		}
		public void ReglasDeIntegridad(){
			db.AssertSinRegistros(
				"La raiz de los grupos en una canasta debe tener el mismo código de la agrupación que define",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is null and grupo<>agrupacion
			");
			db.AssertSinRegistros(
				"Solo la raiz de los grupos en una canasta debe tener el mismo código de la agrupación que define",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is not null and grupo=agrupacion
			");
			db.AssertSinRegistros(
				"Todos los grupos deben tener nivel",
			@"
				SELECT *
				  FROM grupos
				  WHERE nivel is null
			");
			db.AssertSinRegistros(
				"La raiz de los grupos debe tener nivel 0",
			@"
				SELECT *
				  FROM grupos
				  WHERE grupopadre is null and nivel<>0
			");
			db.AssertSinRegistros(
				"Los niveles de los hijos deben ser exactamente 1 más que el padre",
			@"
				SELECT h.grupopadre, p.nivel as nivelpadre, h.grupo, h.nivel, p.nombregrupo as nombrepadre, h.nombregrupo, p.nivel+1-h.nivel
				  FROM grupos p inner join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.nivel+1<>h.nivel
			");
			db.AssertSinRegistros(
				"Todas las agrupaciones que no son productos deben tener hijos",
			@"
				SELECT p.grupo,p.nombregrupo
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.esproducto='N'
				    AND h.grupo is null
			");
			db.AssertSinRegistros(
				"Todas las agrupaciones que son productos no deben tener hijos",
			@"
				SELECT p.grupo,p.nombregrupo,h.grupo as grupohijo,h.nombregrupo as nombrehijo
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.esproducto='S'
				    AND h.grupo is not null
			");
			db.AssertSinRegistros(
				"La suma de los ponderadores debe ser igual al poderador del padre",
			@"
				SELECT p.grupo,p.nombregrupo,p.ponderador,sum(h.ponderador)
				  FROM grupos p left join grupos h on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  GROUP BY p.grupo,p.nombregrupo,p.ponderador
				  HAVING abs(p.ponderador)-sum(h.ponderador)>0.00000000000001
			");
			db.AssertSinRegistros(
				"Solo deben ser hojas los productos",
			@"
				SELECT g.grupo, g.nombregrupo, p.producto, p.nombreproducto
				  FROM grupos g inner join productos p ON g.grupo=p.producto
				  WHERE g.esproducto='N'
			");
			db.AssertSinRegistros(
				"Si esta marcado como producto debe existir el producto",
			@"
				SELECT g.grupo, g.nombregrupo, p.producto, p.nombreproducto
				  FROM grupos g left join productos p ON g.grupo=p.producto
				  WHERE g.esproducto='S' AND p.producto is null
			");
			db.AssertSinRegistros(
				"Los ponderadores de cada nivel deben sumar 1",
			@"
				SELECT g.agrupacion,n.numero as nivel, sum(ponderador) as suma, sum(ponderador)-1 as diferencia
				  FROM numeros as n, grupos as g
				  WHERE g.nivel=n.numero OR g.esproducto='S' AND g.nivel<n.numero
				  GROUP BY g.agrupacion,n.numero
				  HAVING sum(ponderador)<>1;
			");
			db.AssertSinRegistros(
				"No debe haber dos códigos de grupo iguales en distintas agrupaciones",
			@"
				SELECT grupo, count(*) as cantidad, min(agrupacion) as primero, max(agrupacion) as ultimo
				  FROM grupos as g
				  WHERE esproducto='N'
				  GROUP BY grupo
				  HAVING count(*)>1
			");
			db.AssertSinRegistros(
				"No debe haber hijos sin padre",
			@"
				SELECT h.grupopadre,h.grupo,h.nombregrupo
				  FROM grupos h left join grupos p on p.grupo=h.grupopadre and p.agrupacion=h.agrupacion
				  WHERE p.grupo IS NULL and h.grupopadre IS NOT NULL;
			");
		}
	}
	[TestFixture]
	public class ProbarIndiceD3{
		RepositorioIndice repo;
		public ProbarIndiceD3(){
			BaseDatos db;
			switch(3){
				case 1: // probar con postgre
					db=PostgreSql.Abrir("127.0.0.1","import2sqlDB","import2sql","sqlimport");
					db.EliminarTablaSiExiste("calgru");
					db.EliminarTablaSiExiste("calpro");
					db.EliminarTablaSiExiste("periodos");
					db.EliminarTablaSiExiste("grupos");
					db.EliminarTablaSiExiste("productos");
					db.EliminarTablaSiExiste("numeros");
					db.EliminarTablaSiExiste("auxgrupos");
					break;
				case 2: // probar con sqlite
					string archivoSqLite="prueba_sqlite.db";
					Archivo.Borrar(archivoSqLite);
					db=SqLite.Abrir(archivoSqLite);
					break;
				case 3: // probar con access
					string archivoMDB="indices_canastaD3.mdb";
					Archivo.Borrar(archivoMDB);
					BdAccess.Crear(archivoMDB);
					db=BdAccess.Abrir(archivoMDB);
					break;
			}
			repo=RepositorioIndice.Crear(db);
			RepositorioIndice.Productos P100=repo.CrearProducto("P100");
			RepositorioIndice.Productos P101=repo.CrearProducto("P101");
			RepositorioIndice.Productos P102=repo.CrearProducto("P102");
			RepositorioIndice.Agrupaciones A=repo.CrearAgrupacion("A");
			RepositorioIndice.Grupos A1=repo.CrearGrupo("A1",A,60);
			RepositorioIndice.Grupos A2=repo.CrearGrupo("A2",A,40);
			RepositorioIndice.Agrupaciones T=repo.CrearAgrupacion("T");
			RepositorioIndice.Grupos TU=repo.AbrirGrupo("T","T");
			repo.CrearHoja(P100,A1,60);
			repo.CrearHoja(P101,A1,40);
			repo.CrearHoja(P102,A2,100);
			repo.CrearHoja(P100,TU,60);
			repo.CrearHoja(P101,TU,40);
			repo.CrearHoja(P102,TU,100);
			repo.CalcularPonderadores(A);
			repo.CalcularPonderadores(T);
		}
		[Test]
		public void VerCanasta(){
			RepositorioIndice.Grupos A=repo.AbrirGrupo("A","A");
			RepositorioIndice.Grupos A1=repo.AbrirGrupo("A","A1");
			RepositorioIndice.Productos P100=repo.AbrirProducto("P100");
			Assert.AreEqual(1.0,A.cPonderador.Valor);
			Assert.AreEqual(0.6,A1.cPonderador.Valor,0.00000001);
			Assert.AreEqual(0.36,P100.Ponderador(A));
			Assert.AreEqual(0.6,P100.Ponderador(A1));
		}
		[Test]
		public void A01CalculosBase(){
			RepositorioIndice.Periodos pAnt=repo.CrearPeriodo(2001,12);
			RepositorioIndice.Productos P100=repo.AbrirProducto("P100");
			RepositorioIndice.Productos P101=repo.AbrirProducto("P101");
			RepositorioIndice.Productos P102=repo.AbrirProducto("P102");
			RepositorioIndice.Agrupaciones A=repo.AbrirAgrupacion("A");
			repo.RegistrarPromedio(pAnt,P100,2.0);
			repo.RegistrarPromedio(pAnt,P101,10.0);
			repo.RegistrarPromedio(pAnt,P102,20.0);
			repo.CalcularMesBase(pAnt,A);
			Assert.AreEqual(100.0,new RepositorioIndice.CalGru(repo.db,pAnt,A).cIndice.Valor);
			RepositorioIndice.Periodos Per1=pAnt.CrearProximo();
			Assert.AreEqual("200112",Per1.cPeriodoAnterior.Valor);
			Assert.AreEqual("200201",Per1.cPeriodo.Valor);
			Assert.AreEqual(2002,Per1.cAno.Valor);
			Assert.AreEqual(1,Per1.cMes.Valor);
			RepositorioIndice.Grupos A1=repo.AbrirGrupo("A","A1");
			RepositorioIndice.Grupos A2=repo.AbrirGrupo("A","A2");
			repo.RegistrarPromedio(Per1,P100,2.0);
			repo.RegistrarPromedio(Per1,P101,10.0);
			repo.RegistrarPromedio(Per1,P102,22.0);
			repo.CalcularCalGru(Per1,A);
			Assert.AreEqual(110.0,new RepositorioIndice.CalGru(repo.db,Per1,A2).cIndice.Valor);
			Assert.AreEqual(104.0,new RepositorioIndice.CalGru(repo.db,Per1,A).cIndice.Valor);
			RepositorioIndice.Periodos Per2=Per1.CrearProximo();
			repo.RegistrarPromedio(Per2,P100,2.2);
			repo.RegistrarPromedio(Per2,P101,11.0);
			repo.RegistrarPromedio(Per2,P102,22.0);
			repo.CalcularCalGru(Per2,A);
			Assert.AreEqual(110.0,new RepositorioIndice.CalGru(repo.db,Per2,A2).cIndice.Valor);
			Assert.AreEqual(110.0,new RepositorioIndice.CalGru(repo.db,Per2,A).cIndice.Valor);
		}
		[Test]
		public void zReglasDeIntegridad(){
			repo.ReglasDeIntegridad();
		}
	}
}
