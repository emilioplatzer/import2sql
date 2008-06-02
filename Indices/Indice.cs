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

using Comunes;
using BasesDatos;
using ModeladorSql;

namespace Indices
{
	public class CodigoVariedad{
		public string Producto;
		public int Especificacion;
		public int Variedad;
		public CodigoVariedad(string Producto, int Especificacion)
			:this(Producto,Especificacion,1){}
		public CodigoVariedad(string Producto, int Especificacion, int Variedad){
			this.Producto=Producto;
			this.Especificacion=Especificacion;
			this.Variedad=Variedad;
		}
		void Quitar(char Caracter,ref int Variable,ref string codigoProcesando){
			int pos=codigoProcesando.IndexOf(Caracter);
			if(pos>=0){
				Variable=int.Parse(codigoProcesando.Substring(pos+1));
				codigoProcesando=codigoProcesando.Substring(0,pos);	
			}else{
				Variable=1;
			}
		}
		public CodigoVariedad(string codigoJunto){
			Quitar('/',ref Variedad,ref codigoJunto);
			Quitar('.',ref Especificacion,ref codigoJunto);
			Producto=codigoJunto;
		}
		public override string ToString()
		{
			return Producto+(Especificacion>1?"."+Especificacion.ToString():"")
				+(Variedad>1?"/"+Variedad.ToString():"");
		}
	}
	public class EjecutadorSql:BasesDatos.EjecutadorSql{
		public EjecutadorSql(BaseDatos db,Parametros[] param)
			:base(db,param)
		{
		}
		public EjecutadorSql(BaseDatos db,params object[] paramPlanos)
			:base(db,paramPlanos)
		{
		}
	}
	public class RepositorioIndice:Repositorio
	{
		public RepositorioIndice(BaseDatos db)
			:base(db)
		{
		}
		public override void CrearTablas(){
			CrearTablas(db,this.GetType().Namespace);
			for(int i=0; i<=20; i++){
				db.ExecuteNonQuery("INSERT INTO numeros (numero) VALUES ("+i.ToString()+");");
			}
		}
		public override void EliminarTablas(){
			EliminarTablas(db,this.GetType().Namespace);
		}
		public void CalcularPonderadores(Agrupaciones agrupacion){
			using(Ejecutador ej=new Ejecutador(db,agrupacion)){
				Grupos grupos=new Grupos();
				Grupos hijos=new Grupos();
				hijos.UsarFk();
				hijos.Alias="hijos";
				Grupos padre=hijos.fkGrupoPadre;
				ej.Ejecutar(
					new SentenciaUpdate(grupos,grupos.cNivel.Es(0),grupos.cPonderador.Es(1.0))
					.Where(grupos.cGrupoPadre.EsNulo())); 
				for(int i=0;i<10;i++){
					ej.Ejecutar(
						new SentenciaUpdate(grupos,grupos.cNivel.Es(i+1))
						.Where(grupos.InPadresWhere(i))
					);
				}
				for(int i=9;i>=0;i--){ // Subir ponderadores nulos
					ej.Ejecutar(
						new SentenciaUpdate(padre,padre.cPonderador.Es(padre.SelectSuma(hijos.cPonderador)))
						.Where(padre.cNivel.Igual(i).And(padre.cPonderador.EsNulo()))
					);
				}
				AuxGrupos auxgrupos=new AuxGrupos();
				for(int i=1;i<10;i++){
					ej.Ejecutar(
						new SentenciaInsert(new AuxGrupos())
						.Select(padre.cAgrupacion,padre.cGrupo,auxgrupos.cPonderadorOriginal.Es(padre.cPonderador),auxgrupos.cSumaPonderadorHijos.EsSuma(hijos.cPonderador))
						.Where(hijos.cNivel.Igual(i))
					);
					AuxGrupos aux=new AuxGrupos();
					aux.EsFkDe(hijos,hijos.cGrupoPadre);
					ej.Ejecutar(
						new SentenciaUpdate(hijos,hijos.cPonderador.Es(hijos.cPonderador.Por(aux.cPonderadorOriginal.Dividido(aux.cSumaPonderadorHijos))))
						.Where(hijos.cNivel.Igual(i))
					);
				}
			}
		}
		public void CalcularPonderadores(string agrupacion){
			Agrupaciones a=new Agrupaciones();
			a.Leer(db,agrupacion);
			CalcularPonderadores(a);
		}
		public void CalcularMesBase(Calculos cal,Agrupaciones agrupacion){
			using(Ejecutador ej=new Ejecutador(db,agrupacion,cal)){
				CalGru cg=new CalGru();
				Grupos g=new Grupos();
				ej.Ejecutar(
					new SentenciaInsert(cg)
					.Select(cg.cPeriodo.Es(cal.cPeriodo.Valor)
					        ,cg.cCalculo.Es(cal.cCalculo.Valor)
					        ,g.cAgrupacion
					        ,g.cGrupo
					        ,cg.cIndice.Es(100.0)
					        ,cg.cFactor.Es(1.0)
					        ,cg.cImputacionGru.Es(Imputaciones.MB)
					       )
				);
			}
		}
		public void CalcularCalGru(Calculos cal,Agrupaciones agrupacion){
			using(Ejecutador ej=new Ejecutador(db,agrupacion,cal)){
				// Tabla a insertar:
				CalGru cg=new CalGru();
				// Tabla base:
				CalProd cp=new CalProd();
				cp.UsarFk();
				Calculos c=cp.fkCalculos;
				CalProd cp0=new CalProd();
				cp0.Alias="cp0";
				cp0.EsFkDe(cp,cp0.cPeriodo.Es(c.cPeriodoAnterior));
				cp0.LiberadaDelContextoDelEjecutador=true;
				CalGru cg0=new CalGru();
				cg0.Alias="cg0";
				cg0.EsFkDe(cp0,cg0.cGrupo.Es(cp0.cProducto),cg0.cAgrupacion.Es(agrupacion.cAgrupacion.Valor));
				cg0.LiberadaDelContextoDelEjecutador=true;
				cg0.UsarFk();
				Grupos g=cg0.fkGrupos;
				ej.Ejecutar(
					new SentenciaInsert(cg)
					.Select(c,cg0.cAgrupacion,g.cGrupo
					        ,cg.cIndice.Es(cg0.cIndice.Por(cp.cPromedioProd.Dividido(cp0.cPromedioProd)))
					        ,cg.cIndiceParcialActual.Es(cg0.cIndice.Por(cp.cPromedioProd.Dividido(cp0.cPromedioProd)))
					        ,cg.cIndiceParcialAnterior.Es(cg0.cIndice)
					        ,cg.cImputacionGru.Es(cp.cImputacionProd)
					        ,cg0.cFactor)
				);
				Grupos gh=new Grupos();
				gh.Alias="gh";
				cg.EsFkDe(gh,cg.cPeriodo.Es(cal.cPeriodo.Valor),cg.cCalculo.Es(cal.cCalculo.Valor));
				Grupos gp=new Grupos();
				gp.EsFkDe(gh,gp.cGrupo.Es(gh.cGrupoPadre));
				gp.Alias="gp";
				CalGru cgp=new CalGru();
				cgp.Alias="cgp";
				for(int i=9;i>=0;i--){
					ej.Ejecutar(
						new SentenciaInsert(cgp)
						.Select(c,gp,
						        cgp.cIndiceParcialActual.Es(Fun.Sum(cg.cIndiceParcialActual.Por(gh.cPonderador)).Dividido(Fun.Sum(gh.cPonderador))),
						        cgp.cIndiceParcialAnterior.Es(Fun.Sum(cg.cIndiceParcialAnterior.Por(gh.cPonderador)).Dividido(Fun.Sum(gh.cPonderador))),
						        cgp.cImputacionGru.Es(Fun.Min(cg.cImputacionGru)))
						.Where(gh.cNivel.Igual(i))
					);
				}
				cg0.NoEsFk();
				cg.EsFkDe(c,cg0);
				gh.EsFkDe(cg0);
				cgp.EsFkDe(c,cgp.cGrupo.Es(gh.cGrupoPadre),cg0);
				var cgne=new CalGru();
				cgne.EsFkDe(c,cg0);
				cgne.LiberadaDelContextoDelEjecutador=true;
				for(int i=1;i<=9;i++){
					ej.Ejecutar(
						new SentenciaInsert(cg)
						.Select(c,cg0.CamposPk(),
						        cg.cIndiceParcialActual.Es(cg0.cIndice.Por(cgp.cIndiceParcialActual.Dividido(cgp.cIndiceParcialAnterior))),
						        cg.cIndiceParcialAnterior.Es(cg0.cIndice),
						        cg.cImputacionGru.Es(Imputaciones.G)
						       )
						.Where(gh.cNivel.Igual(i),
						       cgne.NoExiste(),
						       cg0.cPeriodo.Igual(c.cPeriodoAnterior) // OJO! Esto deber'ia calcularse automaticamente. Pero falta indicarselo arriba en alguna fk
						      )
						/*
INSERT INTO calgru (periodo, calculo, agrupacion, grupo,
 indiceparcialactual, indiceparcialanterior, imputaciongru)
SELECT x.periodo, x.calculo, x.agrupacion, x.grupo, x.indiceparcialactual, x.indiceparcialanterior, x.imputaciongru
FROM (
 SELECT cal.periodo, cal.calculo, cg0.agrupacion, cg0.grupo,
 cg0.indice*cgp.indiceparcialactual/cgp.indiceparcialanterior AS indiceparcialactual, cg0.indice AS indiceparcialanterior, 'G' AS imputaciongru
 FROM calculos cal, calgru cg0, calgru cgp, grupos gh
 WHERE gh.nivel=2
 AND cg0.periodo=cal.periodoanterior
 AND gh.agrupacion=cg0.agrupacion
 AND gh.grupo=cg0.grupo
 AND cgp.periodo=cal.periodo
 AND cgp.calculo=cal.calculo
 AND cgp.agrupacion=cg0.agrupacion
 AND cgp.grupo=gh.grupopadre
 AND cal.periodo='a2008m05s3'
 AND cal.calculo=0
 AND cgp.agrupacion='C'
 AND cgp.periodo='a2008m05s3'
 AND cgp.calculo=0
 AND gh.agrupacion='C' ) x 
LEFT JOIN calgru z ON
 x.periodo=z.periodo
 AND x.calculo=z.calculo
 AND x.agrupacion=z.agrupacion
 AND x.grupo=z.grupo
WHERE z.periodo is null
						 */
					);
				}
				g.EsFkDe(cg);
				ej.Ejecutar(
					new SentenciaUpdate(cg,cg.cIndice.Es(cg.cIndiceParcialActual))
					.Where(g.cEsProducto.Igual(true))
				);
				var cg_agrup=new CalGru();
				cg_agrup.SubSelect("cgag"
				                   ,cg_agrup.cIndice.Es(Fun.Sum(cg.cIndice.Por(g.cPonderador)).Dividido(Fun.Sum(g.cPonderador)))
				                   ,cg_agrup.cImputacionGru.EsMin(cg.cImputacionGru)
				                   ,cg_agrup.cGrupo.Es(g.cGrupoPadre)
				                   ,cg_agrup.cFactor.Es(Fun.Sum(cg.cFactor.Por(g.cPonderador)).Dividido(Fun.Sum(g.cPonderador)))
				                   ,cgp);
				cgp.EsFkDe(cg,cgp.cGrupo.Es(g.cGrupoPadre));
				cg_agrup.EsFkDe(cg,cg_agrup.cGrupo.Es(g.cGrupoPadre));
				for(int i=9;i>=0;i--){
					if(db is BdAccess){
						ej.EjecutrarSecuencia(
@"DROP VIEW subselect_cgag;
CREATE VIEW subselect_cgag AS
 SELECT SUM(calgru.indice*gr.ponderador)/SUM(gr.ponderador) AS indice,
 MIN(calgru.imputaciongru) AS imputaciongru, 
 gr.grupopadre AS grupo, calgru.periodo, calgru.calculo,
 calgru.agrupacion
 FROM calgru, grupos gr
 WHERE gr.agrupacion=calgru.agrupacion
 AND gr.grupo=calgru.grupo
 AND calgru.agrupacion='A'
 AND calgru.periodo='a2002m01'
 AND calgru.calculo=0
 AND gr.agrupacion='A'
 GROUP BY gr.grupopadre, calgru.periodo, calgru.calculo,
 calgru.agrupacion;
UPDATE calgru
 INNER JOIN grupos gr ON calgru.agrupacion=gr.agrupacion AND calgru.grupo=gr.grupo
SET calgru.indice=
  DLookUp('indice','subselect_cgag','periodo=''' & calgru.periodo & ''' AND calculo=' & calgru.calculo & ' AND agrupacion=''' & gr.agrupacion & ''' AND grupo=''' & gr.grupo & '''') , 
calgru.imputaciongru=
  DLookUp('imputaciongru','subselect_cgag','periodo=''' & calgru.periodo & ''' AND calculo=' & calgru.calculo & ' AND agrupacion=''' & calgru.agrupacion & ''' AND grupo=''' & gr.grupo & '''') 
WHERE gr.esproducto='N'
 AND gr.nivel=9
 AND calgru.agrupacion='A'
 AND calgru.periodo='a2002m01'
 AND calgru.calculo=0
 AND gr.agrupacion='A'
".Replace("a2002m01",cal.cPeriodo.Valor)
.Replace("nivel=9","nivel="+i)
.Replace("calculo=0","calculo="+cal.cCalculo.Valor)
.Replace("agrupacion='A'","agrupacion='"+agrupacion.cAgrupacion.Valor+"'")
);
					}else{
						ej.Ejecutar(
							new SentenciaUpdate(cg,cg.cIndice.Es(cg_agrup.cIndice),cg.cImputacionGru.Es(cg_agrup.cImputacionGru))
							.Where(g.cEsProducto.Igual(false),g.cNivel.Igual(i))
						);
					}
				}
				cg.NoEsFk();
				c.EsFkDe(cg);
				cg0.EsFkDe(cg,cg0.cPeriodo.Es(c.cPeriodoAnterior));
				var cg0ng=new CalGru();
				cg0ng.EsFkDe(cg,cg0ng.cGrupo.Es(cg.cAgrupacion),cg0ng.cPeriodo.Es(c.cPeriodoAnterior)); // !OJO Definici'on ambigua de nivel general
				cg0ng.Alias="cNG";
				cg0ng.LiberadaDelContextoDelEjecutador=true;
				g.EsFkDe(cg);
				ej.Ejecutar(
					new SentenciaUpdate(cg
					                    ,cg.cIncidencia.Es(cg.cIndice.Menos(cg0.cIndice).Por(g.cPonderador).Por(100.0).Dividido(cg0ng.cIndice)))
					.Where(c.cPeriodo.Igual(c.cPeriodo)) // OJO esta incluisi'on la deber'ia calcular automaticamente
				);
			}
		}
		public void CalcularPreciosPeriodo(Calculos cal,bool ControlarPeriodoAnterior){
			System.Console.WriteLine("Calculando promedios de precios e imputaciones del periodo "+cal.cPeriodo.Valor);
			using(Ejecutador ej=new Ejecutador(db,cal)){
				{
					Calculos c=new Calculos();
					if(ControlarPeriodoAnterior){
						ej.AssertSinRegistros(
							"El periodo tiene que tener un periodo anterior"
							,new SentenciaSelect(c.cCalculo,c.cPeriodo,c.cPeriodoAnterior).Where(c.cPeriodoAnterior.EsNulo())
						);
					}
					NovEspInf nei=new NovEspInf();
					CalEspInf cei=new CalEspInf();
					CalEspInf cei0=new CalEspInf();
					cei0.LiberadaDelContextoDelEjecutador=true;
					cei0.Alias="cei0";
					RelVar rv=new RelVar();
					ej.Ejecutar(
						new SentenciaInsert(cei)
						.Select(c.cPeriodo,
						        cei.cPromedioEspInf.SeaNulo(),
								cei.cAntiguedadConPrecio.Es(cei0.cAntiguedadConPrecio.Mas(1)),
						        cei.cAntiguedadSinPrecio.Es(cei0.cAntiguedadSinPrecio.Mas(1)),
						        cei0)
						.Where(c.SiguienteDe(cei0))
					);
					nei.UsarFk();
					Informantes i=nei.fkInformantes;
					cei.EsFkDe(nei);
					ej.Ejecutar(
						new SentenciaInsert(cei)
						.Select(nei,cei.cImputacionEspInf.Es(Imputaciones.B),i)
						.Where(cei.NoExistePara(nei),nei.cEstado.Igual(NovEspInf.Estados.Alta).Or(nei.cEstado.Igual(NovEspInf.Estados.Reemplazo)))
					);
					/*
					CalEspInf ceiss=new CalEspInf();
					ceiss.SubSelect("ceiss",rv.cPeriodo,ceiss.cCalculo.Es(cal.cCalculo.Valor),ceiss.cPromedioEspInf.EsPromedioGeometrico(rv.cPrecio),rv.cInformante)
						.Where(rv.cPrecio.Mayor(Constante<double>.Cero));
					cei.EsFkDe(ceiss);
					*/
					if("metodo directo"=="ahora no"){
						RelVar rv2=new RelVar();
						rv2.Alias="rv2";
						cei.EsFkDe(rv2,cei.cCalculo.Es(cal.cCalculo.Valor),cal);
						ej.Ejecutar( // Calcular el promedio si hay
							new SentenciaUpdate(cei,cei.cPromedioEspInf.Es(cei.SelectPromedioGeometrico(rv2.cPrecio))
						                           ,cei.cImputacionEspInf.Es(Imputaciones.R))
						);
					}else{
						CalEspInfTemp ceit=new CalEspInfTemp();
						ej.Ejecutar(
							new SentenciaDelete(ceit)
						);
						i.EsFkDe(rv);
						ej.Ejecutar(
							new SentenciaInsert(ceit)
							.Select(ceit.cPromedioEspInf.EsPromedioGeometrico(rv.cPrecio)
							        ,ceit.cImputacionEspInf.Es(Imputaciones.R)
							        ,rv,ceit.cCalculo.Es(cal.cCalculo.Valor),i)
							.Where(rv.cPrecio.Mayor(0.0)) // OJO deber'ia deducirlo
						);
						ceit.EsFkDe(cei);
						ej.Ejecutar(
							new SentenciaUpdate(cei,cei.cPromedioEspInf.Es(ceit.cPromedioEspInf)
						                           ,cei.cImputacionEspInf.Es(ceit.cImputacionEspInf))
						);
					}
					CalEspTI ce=new CalEspTI();
					CalEspInf cei1=new CalEspInf();
					cei1.UsarFk();
					cei1.EsFkDe(cei0,cei1.cPeriodo.Es(c.cPeriodo));
					ej.Ejecutar( // Calcular los relativos de imputación
					    new SentenciaInsert(ce)
					    .Select(c,cei1,ce.cPromedioEspMatchingActual.EsPromedioGeometrico(cei1.cPromedioEspInf),
					            ce.cPromedioEspMatchingAnterior.EsPromedioGeometrico(cei0.cPromedioEspInf)
					            ,ce.cImputacionEspTI.Es(Imputaciones.CI))
					    .Where(c.SiguienteDe(cei0),cei1.cPromedioEspInf.Mayor(Constante<double>.Cero),cei0.cPromedioEspInf.Mayor(Constante<double>.Cero))
					);
				}
				{ // Imputación 
					CalEspInf cei=new CalEspInf();
					cei.UsarFk();
					// Informantes i=cei.fkInformantes;
					CalEspTI ce=new CalEspTI();
					Calculos c=new Calculos();
					CalEspInf cei0=new CalEspInf();
					cei0.Alias="cei0";
					ce.EsFkDe(cei); // ,ce.cTipoInformante.Es(i.cTipoInformante));
					c.EsFkDe(cei);
					cei0.EsFkDe(cei,cei0.cPeriodo.Es(c.cPeriodoAnterior));
					cei0.LiberadaDelContextoDelEjecutador=true;
					if(db is BdAccess){
						ej.ExecuteNonQuery(
							@"UPDATE (((calespinf c INNER JOIN informantes i ON c.informante=i.informante) 
INNER JOIN calespti ca ON c.periodo=ca.periodo AND c.calculo=ca.calculo AND c.producto=ca.producto AND c.especificacion=ca.especificacion AND i.tipoinformante=ca.tipoinformante)
INNER JOIN calculos calc ON c.periodo=calc.periodo AND c.calculo=calc.calculo)
INNER JOIN calespinf cal ON calc.periodoanterior=cal.periodo AND c.calculo=cal.calculo AND c.producto=cal.producto AND c.especificacion=cal.especificacion AND c.informante=cal.informante
SET c.promedioespinf=ca.promedioespmatchingactual/(ca.promedioespmatchinganterior/(cal.promedioespinf)),
c.imputacionespinf='IP'
WHERE c.promedioespinf IS NULL
AND calc.periodoanterior=cal.periodo
AND calc.calculo=cal.calculo
AND c.periodo='"+cal.cPeriodo.Valor+@"'
AND c.calculo="+cal.cCalculo.Valor
						);
						ej.ExecuteNonQuery(
							@"UPDATE ((((calespinf c INNER JOIN informantes i ON c.informante=i.informante) INNER JOIN tipoinf ti ON i.tipoinformante=ti.tipoinformante) 
INNER JOIN calespti ca ON c.periodo=ca.periodo AND c.calculo=ca.calculo AND c.producto=ca.producto AND c.especificacion=ca.especificacion AND ti.otrotipoinformante=ca.tipoinformante)
INNER JOIN calculos calc ON c.periodo=calc.periodo AND c.calculo=calc.calculo)
INNER JOIN calespinf cal ON calc.periodoanterior=cal.periodo AND c.calculo=cal.calculo AND c.producto=cal.producto AND c.especificacion=cal.especificacion AND c.informante=cal.informante
SET c.promedioespinf=ca.promedioespmatchingactual/(ca.promedioespmatchinganterior/(cal.promedioespinf)),
c.imputacionespinf='IOTI'
WHERE c.promedioespinf IS NULL
AND calc.periodoanterior=cal.periodo
AND calc.calculo=cal.calculo
AND c.periodo='"+cal.cPeriodo.Valor+@"'
AND c.calculo="+cal.cCalculo.Valor
						);
					}else{
						ej.Ejecutar(
								new SentenciaUpdate(cei,cei.cPromedioEspInf.Es(cei0.cPromedioEspInf.Por(ce.cPromedioEspMatchingActual.Dividido(ce.cPromedioEspMatchingAnterior))),cei.cImputacionEspInf.Es(Imputaciones.IP))
							.Where(cei.cPromedioEspInf.EsNulo()
							       // ,i.cInformante.Igual(i.cInformante)
							       ,c.cPeriodo.Igual(c.cPeriodo) // OJO condiciones dummy para que coloque las tablas en el FROM
							      )
						);
						var ceo=new CalEspTI(); // del otro tipo de informante
						TipoInf ti=cei.fkTipoInf;
						ceo.EsFkDe(cei,ceo.cTipoInformante.Es(ti.cOtroTipoInformante)); // ,ce.cTipoInformante.Es(i.cTipoInformante));
						ej.Ejecutar(
								new SentenciaUpdate(cei,cei.cPromedioEspInf.Es(cei0.cPromedioEspInf.Por(ceo.cPromedioEspMatchingActual.Dividido(ceo.cPromedioEspMatchingAnterior))),cei.cImputacionEspInf.Es(Imputaciones.IOTI))
							.Where(cei.cPromedioEspInf.EsNulo()
							       // ,i.cInformante.Igual(i.cInformante)
							       ,ti.cTipoInformante.Igual(ti.cTipoInformante)
							       ,c.cPeriodo.Igual(c.cPeriodo) // OJO condiciones dummy para que coloque las tablas en el FROM
							      )
						);
					}
				}
				{
					CalEspInf cei1=new CalEspInf();
					cei1.UsarFk();
					Informantes i=cei1.fkInformantes;
					CalEspTI ce=new CalEspTI();
					ce.EsFkDe(cei1);
					CalEspTI ce0=new CalEspTI();
					ce0.EsFkDe(cei1);
					ej.Ejecutar( // Insertar registros cuando no haya promedios
					    new SentenciaInsert(ce)
					    .Select(ce.cImputacionEspTI.Es(Imputaciones.B),cei1,cei1)
					    .Where(ce0.NoExistePara(cei1))
					    .GroupBy()
					);
				}
				{
					CalEspTI ce=new CalEspTI();
					CalEspInf cei=new CalEspInf();
					cei.UsarFk();
					// Informantes i=cei.fkInformantes;
					ce.EsFkDe(cei); // ,ce.cTipoInformante.Es(i.cTipoInformante));
					ej.Ejecutar(
						new SentenciaUpdate(ce
						                    ,ce.cPromedioEsp.Es(ce.SelectPromedioGeometrico(cei.cPromedioEspInf))
						                    ,ce.cImputacionEspTI.Es(ce.SelectMin(cei.cImputacionEspInf)))
					);
				}
				if(cal.cCalculo.Valor==-1){
					ProdTipoInf pit=new ProdTipoInf();
					CalEspTI ce=new CalEspTI();
					pit.EsFkDe(ce);
					ProdTipoInf pit0=new ProdTipoInf();
					pit0.EsFkDe(ce);
					ej.Ejecutar(
						new SentenciaInsert(pit)
						.Select(ce,pit.cPonderadorTI.Es(1.0))
						.Where(pit0.NoExistePara(ce))
						.GroupBy()
					);
				}
				{
					CalProdTI ct=new CalProdTI();
					CalEspTI ce=new CalEspTI();
					ce.UsarFk();
					Especificaciones e=ce.fkEspecificaciones;
					ct.EsFkDe(ce,ct.cProducto.Es(e.cProducto));
					ej.Ejecutar(
						new SentenciaInsert(ct)
						.Select(ce,e,ct.cPromedioProdTI.EsPromedioGeometrico(ce.cPromedioEsp),ct.cImputacionProdTI.EsMin(ce.cImputacionEspTI))
						.Where(ce.cPromedioEsp.Mayor(0.0))
					);
				}
				{
					CalProd cp=new CalProd();
					CalProdTI cpt=new CalProdTI();
					cp.EsFkDe(cpt);
					ej.Ejecutar(
						new SentenciaInsert(cp)
						.Select(cpt,cp.cPromedioProd.EsPromedioGeometrico(cpt.cPromedioProdTI)
						        ,cp.cImputacionProd.EsMin(cpt.cImputacionProdTI))
					);
				}
			}
		}
		public void CalcularMatrizBase(int CantidadPeriodosMinima){
			System.Console.WriteLine("Calculando Matriz base ");
			{
				NovEspInf n=new NovEspInf();
				Calculos c=new Calculos();
				RelVar rv=new RelVar();
				c.EsFkDe(rv,c.cCalculo.Es(-1));
				NovEspInf nss=new NovEspInf();
				nss.SubSelect("nss",rv.cPeriodo,c.cCalculo,rv.cInformante,rv.cProducto,rv.cEspecificacion)
				.Where(c.cEsPeriodoBase.Igual(true),rv.cPrecio.NoEsNulo())
				.GroupBy();
				new Ejecutador(db).Ejecutar(
					new SentenciaInsert(n)
					.Select(n.cPeriodo.EsMax(nss.cPeriodo),n.cEstado.Es(NovEspInf.Estados.Alta),nss) // decía nss.cCalculo,nss.cInformante,nss.cEspecificacion
					.Having(Fun.Count(nss.cPeriodo).MayorOIgual<int>(CantidadPeriodosMinima))
				);
			}
			Calculos cals=new Calculos();
			string ultimoCodigoPeriodo="";
			foreach(Calculos cal in new Calculos().Algunos(db,cals.cEsPeriodoBase.Igual(true),cals.cPeriodo.Desc())){
				CalcularPreciosPeriodo(cal,!cal.cPeriodoAnterior.ContieneNull);
				ultimoCodigoPeriodo=cal.cPeriodo.Valor;
			}
			System.Console.WriteLine("Copia del último periodo");
			var calu=new Calculos();
			calu.Leer(db,ultimoCodigoPeriodo,-1);
			using(var ej=new Ejecutador(db,calu)){
				var cal0=new Calculos();
				cal0.Leer(db,"a0000m00",0);
				var cei=new CalEspInf();
				var cei0=new CalEspInf();
				ej.Ejecutar(
					new SentenciaInsert(cei)
					.Select(cei.cPeriodo.Es(cal0.cPeriodo.Valor),cei.cCalculo.Es(cal0.cCalculo.Valor),cei0)
				);
				System.Console.WriteLine("Periodo base = 100");
				var cg=new CalGru();
				var g=new Grupos();
				var cp0=new CalProd();
				ej.Ejecutar(
					new SentenciaInsert(cg)
					.Select(cg.cPeriodo.Es(cal0.cPeriodo.Valor),cg.cCalculo.Es(cal0.cCalculo.Valor),cg.cIndice.Es(100.0),cg.cFactor.Es(1.0),cg.cImputacionGru.Es(Imputaciones.MB),g)
				);
				ej.Ejecutar(
					new SentenciaInsert(cp0)
					.Select(cp0.cPeriodo.Es(cal0.cPeriodo.Valor),cp0.cCalculo.Es(cal0.cCalculo.Valor),cp0.cImputacionProd.Es(Imputaciones.MB),cp0)
				);
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
				  HAVING abs(sum(ponderador)-1)>0.000000001;
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
	public class RepositorioPruebaIndice:RepositorioIndice{
		public RepositorioPruebaIndice(BaseDatos db)
			:base(db)
		{}
		public override void CrearTablas(){
			RepositorioIndice repo=new RepositorioIndice(db);
			repo.CrearTablas();
		}
		public override void EliminarTablas(){
			RepositorioIndice repo=new RepositorioIndice(db);
			repo.EliminarTablas();
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
			}			
		}
		public static Periodos CrearPeriodo(BaseDatos db,int ano, int mes){
			Periodos p=new Periodos();
			using(Insertador ins=p.Insertar(db)){
				p.cPeriodo[ins]="a"+ano.ToString()+"m"+mes.ToString("00");
				p.cAno[ins]=ano;
				p.cMes[ins]=mes;
			}
			return new Periodos(db,ano,mes);
		}
		public Periodos CrearPeriodo(int ano, int mes){
			return CrearPeriodo(db,ano,mes);
		}
		public Periodos CrearProximo(Periodos ant){
			int ano=ant.cAno.Valor;
			int mes=ant.cMes.Valor+1;
			if(mes==13){
				mes=1; 
				ano++;
			}
			Periodos p=new Periodos();
			string codigoNuevo="a"+ano.ToString()+"m"+((int)mes).ToString("00");
			if(!p.Buscar(db,codigoNuevo)){
				using(Insertador ins=p.Insertar(db)){
					p.cPeriodo[ins]=codigoNuevo;
					p.cAno[ins]=ano;
					p.cMes[ins]=mes;
				}
			}
			return new Periodos(db,ano,mes);
		}
		public Periodos AbrirPeriodo(int ano, int mes){
			Periodos p=new Periodos();
			p.LeerNoPk(db,"ano",ano,"mes",mes);
			return p;
		}
		public Calculos CrearCalculo(int ano, int mes, int calculo){
			Periodos p=CrearPeriodo(ano,mes);
			Calculos c=new Calculos();
			using(Insertador ins=c.Insertar(db)){
				c.cPeriodo[ins]=p.cPeriodo.Valor;
				c.cCalculo[ins]=calculo;
			}
			return AbrirCalculo(ano,mes,calculo);
		}
		public Calculos AbrirCalculo(int ano, int mes, int calculo){
			Periodos p=AbrirPeriodo(ano,mes);
			Calculos c=new Calculos();
			c.Leer(db,p.cPeriodo.Valor,calculo);
			return c;
		}
		public Calculos CrearProximo(Calculos ant){
			Periodos p=new Periodos();
			p.Leer(db,ant.cPeriodo);
			Periodos pProx=CrearProximo(p);
			Calculos c=new Calculos();
			using(Insertador ins=c.Insertar(db)){
				c.cPeriodo[ins]=pProx.cPeriodo.Valor;
				c.cCalculo[ins]=ant.cCalculo.Valor;
				c.cPeriodoAnterior[ins]=ant.cPeriodo;
				c.cEsPeriodoBase[ins]=ant.cEsPeriodoBase;
			}
			c.Leer(db,pProx.cPeriodo.Valor,ant.cCalculo.Valor);
			return c;
		}
		public void RegistrarPromedio(Calculos cal,Productos prod,double promedio){
			CalProd t=new CalProd();
			using(Insertador ins=t.Insertar(db)){
				t.cPeriodo[ins]=cal.cPeriodo;
				t.cCalculo[ins]=cal.cCalculo;
				t.cProducto[ins]=prod.cProducto;
				t.cPromedioProd[ins]=promedio;
				t.cImputacionProd[ins]=Imputaciones.R;
			}		
		}
		public void ExpandirEspecificacionesYVariedades(){
			Productos p=new Productos();
			Especificaciones e=new Especificaciones();
			Variedades v=new Variedades();
			Ejecutador ej=new Ejecutador(db);
			ej.Ejecutar(
				new SentenciaInsert(e).Select(e.cEspecificacion.Es(1),p)
			);
			ej.Ejecutar(
				new SentenciaInsert(v).Select(v.cVariedad.Es(1),e)
			);
		}
	}
	[TestFixture]
	public class ProbarIndiceD3{
		RepositorioPruebaIndice repo;
		public ProbarIndiceD3(){
			BaseDatos db;
			#pragma warning disable 162
			switch(3){ // No anda con sqlite hasta que no implemente EXP 
				case 1: // probar con postgre
					db=PostgreSql.Abrir("127.0.0.1","import2sqlDB","import2sql","sqlimport");
					repo=new RepositorioPruebaIndice(db);
					repo.EliminarTablas();
					repo.CrearTablas();
					break;
				case 2: // probar con sqlite
					string archivoSqLite="prueba_sqlite.db";
					Archivo.Borrar(archivoSqLite);
					db=SqLite.Abrir(archivoSqLite);
					repo=new RepositorioPruebaIndice(db);
					repo.CrearTablas();
					break;
				case 3: // probar con access
					string archivoMDB="indices_canastaD3.mdb";
					Archivo.Borrar(archivoMDB);
					BdAccess.Crear(archivoMDB);
					db=BdAccess.Abrir(archivoMDB);
					repo=new RepositorioPruebaIndice(db);
					repo.CrearTablas();
					break;
			}
			#pragma warning restore 162
			Productos P100=new Productos(); P100.InsertarDirecto(db,"P100");
			Productos P101=repo.CrearProducto("P101");
			Productos P102=repo.CrearProducto("P102");
			Productos P103=repo.CrearProducto("P103");
			Agrupaciones A=repo.CrearAgrupacion("A");
			Grupos A1=repo.CrearGrupo("A1",A,60);
			Grupos A2=repo.CrearGrupo("A2",A,40);
			Agrupaciones T=repo.CrearAgrupacion("T");
			Grupos TU=repo.AbrirGrupo("T","T");
			repo.CrearHoja(P100,A1,60);
			repo.CrearHoja(P101,A1,40);
			repo.CrearHoja(P102,A2,100);
			repo.CrearHoja(P103,A2,100);
			repo.CrearHoja(P100,TU,60);
			repo.CrearHoja(P101,TU,40);
			repo.CrearHoja(P102,TU,100);
			repo.CalcularPonderadores(A);
			repo.CalcularPonderadores(T);
		}
		[Test]
		public void A01CalculosEstructuraBase(){
			Calculos pAnt=repo.CrearCalculo(2001,12,0);
			using(var ej=new Ejecutador(repo.db,pAnt)){
				var p0=new Periodos();
				p0.InsertarDirecto(repo.db,"a0000m00",0,0);
				var c0=new Calculos();
				c0.InsertarDirecto(repo.db,"a0000m00",0);
				ej.Ejecutar(
					new SentenciaUpdate(c0,c0.cPeriodoAnterior.Es("a0000m00"))
				);
			}
			// Calculos pAnt=repo.CrearCalculo(2001,12,0);
			Productos P100=repo.AbrirProducto("P100");
			Productos P101=repo.AbrirProducto("P101");
			Productos P102=repo.AbrirProducto("P102");
			Productos P103=repo.AbrirProducto("P103");
			Agrupaciones A=repo.AbrirAgrupacion("A");
			repo.RegistrarPromedio(pAnt,P100,2.0);
			repo.RegistrarPromedio(pAnt,P101,10.0);
			repo.RegistrarPromedio(pAnt,P102,20.0);
			repo.CalcularMesBase(pAnt,A);
			Assert.AreEqual(100.0,new CalGru(repo.db,pAnt,A).cIndice.Valor);
			Calculos Per1=repo.CrearProximo(pAnt);
			Per1.UsarFk();
			Assert.AreEqual("a2002m01",Per1.cPeriodo.Valor);
			Assert.AreEqual(2002,Per1.fkPeriodos.cAno.Valor);
			Assert.AreEqual(1,Per1.fkPeriodos.cMes.Valor);
			Grupos A1=repo.AbrirGrupo("A","A1");
			Grupos A2=repo.AbrirGrupo("A","A2");
			Grupos AP103=repo.AbrirGrupo("A","P103");
			repo.RegistrarPromedio(Per1,P100,2.0);
			repo.RegistrarPromedio(Per1,P101,10.0);
			repo.RegistrarPromedio(Per1,P102,22.0);
			repo.CalcularCalGru(Per1,A);
			Assert.AreEqual(110.0,new CalGru(repo.db,Per1,A2).cIndice.Valor,Controlar.DeltaDouble); 
			Assert.AreEqual(104.0,new CalGru(repo.db,Per1,A).cIndice.Valor,Controlar.DeltaDouble);
			/*
			var cgP103=new CalGru();
			Assert.IsFalse(cgP103.Buscar(repo.db,"a2002m01",0,"A","P103"));
			*/
			Assert.AreEqual(110.0,new CalGru(repo.db,Per1,AP103).cIndice.Valor,Controlar.DeltaDouble);
			Calculos Per2=repo.CrearProximo(Per1);
			repo.RegistrarPromedio(Per2,P100,2.2);
			repo.RegistrarPromedio(Per2,P101,11.0);
			repo.RegistrarPromedio(Per2,P102,22.0);
			repo.CalcularCalGru(Per2,A);
			Assert.AreEqual(110.0,new CalGru(repo.db,Per2,A2).cIndice.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(110.0,new CalGru(repo.db,Per2,A).cIndice.Valor,Controlar.DeltaDouble);
			repo.ExpandirEspecificacionesYVariedades();
			Calculos Per3=repo.CrearProximo(Per2);
			repo.RegistrarPromedio(Per3,P100,2.4);
			repo.RegistrarPromedio(Per3,P102,24.0);
			repo.CalcularCalGru(Per3,A);
			Assert.AreEqual(120.0,new CalGru(repo.db,Per3,A1).cIndice.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(120.0,new CalGru(repo.db,Per3,A2).cIndice.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(120.0,new CalGru(repo.db,Per3,A).cIndice.Valor,Controlar.DeltaDouble);
		}
		public void CargarPrecio(string periodo, string producto, int informante, double precio){
			RelVar r=new RelVar();
			CodigoVariedad codvar=new CodigoVariedad(producto);
			r.InsertarValores(repo.db,r.cPeriodo.Es(periodo),r.cInformante.Es(informante),r.cProducto.Es(codvar.Producto),r.cEspecificacion.Es(codvar.Especificacion),r.cVariedad.Es(codvar.Variedad),r.cPrecio.Es(precio));
		}
		[Test]
		public void A02CalculosMatrizBase(){
			using(var ej=new Ejecutador(repo.db)){
				ej.Ejecutar(new SentenciaDelete(new CalEspInf()));
				ej.Ejecutar(new SentenciaDelete(new CalEspTI()));
				ej.Ejecutar(new SentenciaDelete(new CalGru()));
				ej.Ejecutar(new SentenciaDelete(new CalProd()));
				ej.Ejecutar(new SentenciaDelete(new CalProdTI()));
			}
			TipoInf ti=new TipoInf();
			ti.InsertarDirecto(repo.db,"T","S");
			ti.InsertarDirecto(repo.db,"S","T");
			Informantes inf=new Informantes();
			inf.InsertarDirecto(repo.db,1,"","T");
			inf.InsertarDirecto(repo.db,2,"","T");
			inf.InsertarDirecto(repo.db,3,"","T");
			inf.InsertarDirecto(repo.db,4,"","T");
			Variedades v=new Variedades();
			v.InsertarValores(repo.db,v.cProducto.Es("P100"),v.cEspecificacion.Es(1),v.cVariedad.Es(2));
			CargarPrecio("a2001m12","P100"	,1,2.0);
			CargarPrecio("a2001m12","P100"	,2,2.0);
			CargarPrecio("a2001m12","P100"	,4,3.0);
			CargarPrecio("a2002m01","P100"	,1,2.0);
			CargarPrecio("a2002m01","P100"	,4,3.0);
			CargarPrecio("a2002m01","P100/2"	,4,3.60);
			CargarPrecio("a2002m02","P100"	,1,2.0);
			CargarPrecio("a2002m02","P100"	,2,2.2);
			CargarPrecio("a2002m02","P100/2"	,1,2.60);
			CargarPrecio("a2002m02","P100"	,3,2.4);
			CargarPrecio("a2002m02","P100/2"	,3,2.60);
			CargarPrecio("a2001m12","P101"	,2,12.2);
			CargarPrecio("a2002m01","P101"	,2,12.2);
			Periodos p=new Periodos(); 
			Calculos c=new Calculos();
			c.cCalculo.AsignarValor(-1);
			c.cEsPeriodoBase.AsignarValor(true);
			c.InsertarValores(repo.db,c.cPeriodo.Es("a2002m02"),c.cEsPeriodoBase,c);
			c.InsertarValores(repo.db,c.cPeriodo.Es("a2002m01"),c.cEsPeriodoBase,c.cPeriodoAnterior.Es("a2002m02"),c);
			c.InsertarValores(repo.db,c.cPeriodo.Es("a2001m12"),c.cEsPeriodoBase,c.cPeriodoAnterior.Es("a2002m01"),c);
			Assert.IsTrue(c.Buscar(repo.db,"a2001m12",-1),"está el primer período");
			Assert.IsTrue(c.Buscar(repo.db,"a2002m02",-1),"está el último período");
			// Producto P103 con imputación por Otros
			inf.InsertarDirecto(repo.db,101,"","S");
			CargarPrecio("a2001m12","P103"	,1,50.0);
			CargarPrecio("a2002m01","P103"	,1,55.0);
			CargarPrecio("a2002m02","P103"	,1,60.0);
			CargarPrecio("a2001m12","P103"	,101,60.0);
			CargarPrecio("a2002m02","P103"	,101,60.0);
			repo.CalcularMatrizBase(2);
			object[,] esperado={ // Novedades (o sea altas del periodo base para el retroceso)
				{"a2002m01","P100"	,4},
				{"a2002m01","P101"	,2},
				{"a2002m02","P100"	,1},
				{"a2002m02","P100"	,2},
				{"a2002m02","P103"	,1},
				{"a2002m02","P103"	,101}
			};
			int cantidad=0;
			foreach(NovEspInf n in new NovEspInf().Todos(repo.db)){
				Assert.IsTrue(cantidad<esperado.GetLength(0),"Hay resultados de más");
				Assert.AreEqual(esperado[cantidad,2],n.cInformante.Valor);
				Assert.AreEqual(esperado[cantidad,0],n.cPeriodo.Valor);
				Assert.AreEqual(esperado[cantidad,1],new CodigoVariedad(n.cProducto.Valor,n.cEspecificacion.Valor).ToString());
				Assert.AreEqual(-1,n.cCalculo.Valor);
				cantidad++;
			}
			Assert.AreEqual(esperado.GetLength(0),cantidad,"cantidad de registros vistos");
			object[,] esperado2={ // precios sin imputaciones en el periodo base en retroceso
				{"a2001m12","P100"	,1,2.0},
				{"a2001m12","P100"	,2,2.0},
				{"a2001m12","P100"	,4,3.0},
				{"a2001m12","P101"	,2,12.2},
				{"a2001m12","P103"	,1,50.0},
				{"a2001m12","P103"	,101,60.0},
				{"a2002m01","P100"	,1,2.0},
				{"a2002m01","P100"	,2,null},
				{"a2002m01","P100"	,4,Math.Sqrt(3.0*3.60)},
				{"a2002m01","P101"	,2,12.2},
				{"a2002m01","P103"	,1,55.0},
				{"a2002m01","P103"	,101,null},
				{"a2002m02","P100"	,1,Math.Sqrt(2.0*2.60)},
				{"a2002m02","P100"	,2,2.2},
				{"a2002m02","P103"	,1,60.0},
				{"a2002m02","P103"	,101,60.0}};
			cantidad=0;
			CalEspInf ceis=new CalEspInf();
			foreach(CalEspInf cei in ceis.Algunos(repo.db,ceis.cCalculo.Igual(-1))){
				Assert.IsTrue(cantidad<esperado2.GetLength(0),"Hay resultados de más");
				// System.Console.WriteLine("Registro {0}={1}",cantidad,cei.MostrarCampos());
				Assert.AreEqual(esperado2[cantidad,2],cei.cInformante.Valor);
				Assert.AreEqual(esperado2[cantidad,0],cei.cPeriodo.Valor);
				Assert.AreEqual(esperado2[cantidad,1],new CodigoVariedad(cei.cProducto.Valor,cei.cEspecificacion.Valor).ToString());
				Assert.AreEqual(-1,cei.cCalculo.Valor);
				if(esperado2[cantidad,3]==null){ 
					// el valor está imputado
				}else{
					Assert.AreEqual((double)esperado2[cantidad,3],(double)cei.cPromedioEspInf.Valor,Controlar.DeltaDouble);
				}
				cantidad++;
			}
			Assert.AreEqual(esperado2.GetLength(0),cantidad,"cantidad de registros vistos");
			CalEspTI ce=new CalEspTI();
			ce.Leer(repo.db,"a2002m01",-1,"P100",1,"T");
			Assert.AreEqual(Math.Sqrt(2.0*2.60),(double)ce.cPromedioEspMatchingAnterior.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(2.0,(double)ce.cPromedioEspMatchingActual.Valor,Controlar.DeltaDouble);
			CalEspInf cei1=new CalEspInf();
			cei1.Leer(repo.db,"a2002m01",-1,"P100",1,2);
			Assert.AreEqual(2.2*(2.0/Math.Sqrt(2.0*2.60)),(double)cei1.cPromedioEspInf.Valor,Controlar.DeltaDouble);
			cei1.Leer(repo.db,"a2002m01",-1,"P103",1,101);
			Assert.AreEqual(Imputaciones.IOTI,cei1.cImputacionEspInf.Valor);
			ce.Leer(repo.db,"a2002m01",-1,"P100",1,"T");
			Assert.AreEqual(Math.Pow(2.0*Math.Sqrt(3.0*3.60)*2.2*(2.0/Math.Sqrt(2.0*2.60)),1.0/3.0),(double)ce.cPromedioEsp.Valor,Controlar.DeltaDouble);
		}
		[Test]
		public void A03CalculosMensuales(){
			var A=new Agrupaciones();
			A.Leer(repo.db,"A");
			var calculos=new Calculos();
			foreach(Calculos cal in calculos.Algunos(repo.db,calculos.cCalculo.Igual(0).And(calculos.cPeriodo.Mayor("a2")))){
				Console.WriteLine("Calculo 0, periodo "+cal.cPeriodo.Valor);
				repo.CalcularPreciosPeriodo(cal,true);
				repo.CalcularCalGru(cal,A);
			}
			var cp=new CalProd();
			cp.Leer(repo.db,"a2001m12",0,"P100");
			Assert.AreEqual(
				Math.Pow(2.0*2.0*3.0,1.0/3.0)
				,cp.cPromedioProd.Valor,
				Controlar.DeltaDouble
			);
			double razonImp12a1_P100=
				Math.Sqrt(2.0*Math.Sqrt(3.0*3.60))/Math.Sqrt(2.0*3.0);
			var cei=new CalEspInf();
			cei.Leer(repo.db,"a2002m01",0,"P100",1,2);
			Assert.AreEqual(
				2.0*razonImp12a1_P100
				,cei.cPromedioEspInf.Valor,
				Controlar.DeltaDouble
			);
			cp.Leer(repo.db,"a2002m01",0,"P100");
			Assert.AreEqual(
				Math.Pow(2.0*2.0*razonImp12a1_P100*Math.Sqrt(3.0*3.60),1.0/3.0)
				,cp.cPromedioProd.Valor,
				Controlar.DeltaDouble
			);
			double razonImp1a2_P100=
				Math.Sqrt(Math.Sqrt(2.0*2.60)*2.2)/Math.Sqrt(2.0*2.0*razonImp12a1_P100);
			cei.Leer(repo.db,"a2002m02",0,"P100",1,4);
			Assert.AreEqual(
				Math.Sqrt(3.0*3.60)*razonImp1a2_P100
				,cei.cPromedioEspInf.Valor,
				Controlar.DeltaDouble
			);
			cp.Leer(repo.db,"a2002m02",0,"P100");
			Assert.AreEqual(
				Math.Pow(Math.Sqrt(2.0*2.60)*2.2*Math.Sqrt(3.0*3.60)*razonImp1a2_P100,1.0/3.0)
				,cp.cPromedioProd.Valor,
				Controlar.DeltaDouble
			);
			var cg=new CalGru();
			cg.Leer(repo.db,"a2001m12",0,"A","A");
			Assert.AreEqual(100.0,cg.cIndice.Valor);
			cg.Leer(repo.db,"a2002m01",0,"A","P101");
			Assert.AreEqual(100.0,cg.cIndice.Valor);
			cg.Leer(repo.db,"a2002m01",0,"A","P100");
			Assert.AreEqual(100.0*Math.Pow(2.0*2.0*razonImp12a1_P100*Math.Sqrt(3.0*3.60),1.0/3.0)
							/Math.Pow(2.0*2.0*3.0,1.0/3.0)
			                ,cg.cIndice.Valor
			               ,Controlar.DeltaDouble);
			cg.Leer(repo.db,"a2002m01",0,"A","A1");
			Assert.AreEqual(100.0*Math.Pow(2.0*2.0*razonImp12a1_P100*Math.Sqrt(3.0*3.60),1.0/3.0)
			                         /Math.Pow(2.0*2.0*3.0,1.0/3.0)*0.36/0.6+
			                         100*0.24/0.6
			                ,cg.cIndice.Valor
			               ,Controlar.DeltaDouble);
			cg.Leer(repo.db,"a2002m02",0,"A","P100");
			Assert.AreEqual(100.0*Math.Pow(Math.Sqrt(2.0*2.60)*2.2*Math.Sqrt(3.0*3.60)*razonImp1a2_P100,1.0/3.0)
							/Math.Pow(2.0*2.0*3.0,1.0/3.0)
			                ,cg.cIndice.Valor
			               ,Controlar.DeltaDouble);
		}
		[Test]
		public void VerCanasta(){
			Grupos A=repo.AbrirGrupo("A","A");
			Grupos A1=repo.AbrirGrupo("A","A1");
			Productos P100=repo.AbrirProducto("P100");
			Assert.AreEqual(1.0,A.cPonderador.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(0.6,A1.cPonderador.Valor,Controlar.DeltaDouble);
			Assert.AreEqual(0.36,P100.Ponderador(A).Value,Controlar.DeltaDouble);
			Assert.AreEqual(0.6,P100.Ponderador(A1).Value,Controlar.DeltaDouble);
		}
		[Test]
		public void zReglasDeIntegridad(){
			repo.ReglasDeIntegridad();
		}
		[Test] 
		public void CodVar(){
			Assert.AreEqual("P100",new CodigoVariedad("P100",1,1).ToString());
			Assert.AreEqual("P100/2",new CodigoVariedad("P100",1,2).ToString());
			Assert.AreEqual("P100.2",new CodigoVariedad("P100",2,1).ToString());
			Assert.AreEqual("P100.2/3",new CodigoVariedad("P100",2,3).ToString());
			Assert.AreEqual("P100",new CodigoVariedad("P100").ToString());
			Assert.AreEqual("P100/2",new CodigoVariedad("P100/2").ToString());
			Assert.AreEqual("P100.2",new CodigoVariedad("P100.2").ToString());
			Assert.AreEqual("P100.2/3",new CodigoVariedad("P100.2/3").ToString());
		}
	}
}
